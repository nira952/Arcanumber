using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public enum GameState
{
    Initialize,
    Ready,
    Start,
    Playing,
    Pause,
    Finish,
}

public class GameManager : NetworkBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    private static readonly Subject<Unit> onInitialized = new();
    public static Observable<Unit> OnInitialized => onInitialized;

    private void SetUpSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            onInitialized.OnNext(Unit.Default);
        }
        else
        {
            Destroy(gameObject);
        }
        Debug.Log("[GameManager] Awake called. Instance set.");
    }
    #endregion

    [SerializeField] private GameUIManager gameUIManager;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private PlayerSpawner playerSpawner;


    // --- NetworkVariable ---
    public NetworkVariable<GameState> NetWorkGameState = new NetworkVariable<GameState>(GameState.Initialize);

    // --- ReactiveProperty ---
    private readonly ReactiveProperty<GameState> stateRx = new ReactiveProperty<GameState>(GameState.Initialize);
    public ReadOnlyReactiveProperty<GameState> StateRx => stateRx;

    private readonly CompositeDisposable playerSubscriptions = new();

    private bool isLocalMode = false;


    private void Awake()
    {
        SetUpSingleton(); // Singletonの設定

        // ローカルモードかどうかを判定
        isLocalMode = PlayerDataManager.Instance.IsLocalMode;
    }


    // ローカルモードのスタートポイント
    private void Start()
    {
        if (!isLocalMode) { return; }
        
        Debug.Log("[GameManager] オフラインモードで起動します。");

        // オフラインモードでは全プレイヤーを一括生成
        playerSpawner.SpawnAllPlayersOffline();

        // プレイヤーのダウン状態を監視する購読を設定
        SettingPlayerObservable();

        // ゲーム開始シーケンスを非同期で開始
        StartGameSequenceAsync().Forget();

    }

    // ネットワークモードのスタートポイント
    public override void OnNetworkSpawn()
    {
        //ローカルモード時は処理しない
        if (isLocalMode) { return; }

        Debug.Log($"[GameManager] ネットワークモードで起動します。IsServer: {IsServer}, IsClient: {IsClient}");


        List<PlayerRoot> Players = PlayerUtility.GetAllPlayer();

        // CurrentStateの変更をReactivePropertyに反映
        NetWorkGameState.AsObservable().Subscribe(state => stateRx.Value = state).AddTo(this);

        // TimeManagerとGameUIManagerの設定
        timeManager.Initialize(this);

        gameUIManager.Initialize(this, timeManager, Players);

        // タイムアップ時の処理を購読
        timeManager.onTimeUp.Subscribe(_ => HandleTimeUp()).AddTo(this);

        // サーバー（ホスト）側のみが、シーンロード完了イベントを監視する
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
        }
    }

    /// <summary>
    /// シーンイベントのハンドラ（サーバー専用）
    /// </summary>
    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        // イベントが「全クライアントのロード完了 (LoadEventCompleted)」かつ「現在のシーン」の場合のみ実行
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            // 該当シーンロードイベントを発生させたのが自シーンかチェック
            if (sceneEvent.SceneName == gameObject.scene.name)
            {
                Debug.Log($"[PlayerSpawner] 全クライアントのシーンロードが完了しました。一括生成を開始します。");

                // 二重実行を防ぐため、一度実行したらイベント解除
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;

                // 全プレイヤーを一括生成
                playerSpawner.SpawnAllPlayersOnline();

                // プレイヤーのダウン状態を監視する購読を設定
                SettingPlayerObservable();

                // ゲーム開始シーケンスを非同期で開始
                StartGameSequenceAsync().Forget();

            }
        }
    }

    // Server専用 : プレイヤーのダウン状態を監視する購読を設定
    private void SettingPlayerObservable()
    {
        // すべてのプレイヤーを取得
        List<PlayerRoot> Players = PlayerUtility.GetAllPlayer();

        foreach (var player in Players)
        {
            if (player == null) continue;
            // プレイヤーのダウン状態を監視する購読を設定
            player.IsDown
                .Where(isDown => isDown)
                .Subscribe(_ =>
                {
                    Debug.Log($"[GameManager] {player.name} がダウンしました。終了条件をチェックします。");
                    CheckFinishCondition();
                })
                .AddTo(playerSubscriptions);
        }
    }


    private void ChangeGameState(GameState newState)
    {
        if (isLocalMode)
        {
            stateRx.Value = newState;
        }
        else if (IsServer)
        {
            NetWorkGameState.Value = newState;
        }
    }

    // ゲーム開始シーケンスを非同期で実行するメソッド
    private async UniTaskVoid StartGameSequenceAsync()
    {
        // TODO : 要改修

        ChangeGameState(GameState.Ready);

        for (int i = 3; i > 0; i--)
        {
            UpdateGameStateTextInternal(i.ToString());
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        ChangeGameState(GameState.Start);
        await UniTask.Yield(PlayerLoopTiming.Update);
        ChangeGameState(GameState.Playing);

        await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: this.GetCancellationTokenOnDestroy());
        HideGameStateTextInternal();
    }   

    private void HandleTimeUp()
    {
        if (!isLocalMode && !IsServer) return;

        ChangeGameState(GameState.Finish);

        // すべてのプレイヤーを取得
        List<PlayerRoot> Players = PlayerUtility.GetAllPlayer();

        if (Players == null || Players.Count == 0) return;

        var alivePlayers = Players.Where(p => p != null && !p.IsDown.Value).ToList();
        string winnerName = DetermineWinnerByHp(alivePlayers);

        TriggerFinishSequenceInternal(winnerName);
    }

    private void CheckFinishCondition()
    {
        if (StateRx.CurrentValue != GameState.Playing) return;
        if (!isLocalMode && !IsServer) return;

        // すべてのプレイヤーを取得
        List<PlayerRoot> players = PlayerUtility.GetAllPlayer();

        if (players == null || players.Count == 0) return;

        int totalPlayers = players.Count;
        int downCount = players.Count(p => p != null && p.IsDown.Value);

        Debug.Log($"[CheckFinishCondition] DownCount: {downCount} / Total: {totalPlayers}");

        if (totalPlayers <= 1)
        {
            if (downCount >= 1)
            {
                ChangeGameState(GameState.Finish);
                TriggerFinishSequenceInternal("Game Over");
            }
        }
        else
        {
            if (downCount >= totalPlayers - 1)
            {
                ChangeGameState(GameState.Finish);

                var winner = players.FirstOrDefault(p => p != null && !p.IsDown.Value);
                string winnerName = winner != null ? PlayerDataManager.Instance.GetPlayerNameByIndex(winner.PlayerIndex.Value) : "Draw";

                TriggerFinishSequenceInternal(winnerName);
            }
        }
    }

    private string DetermineWinnerByHp(List<PlayerRoot> alivePlayers)
    {
        if (alivePlayers.Count == 0)
            return isLocalMode ? "Game Over" : "Draw";

        if (alivePlayers.Count == 1)
            return PlayerDataManager.Instance.GetPlayerNameByIndex(alivePlayers[0].PlayerIndex.Value);

        float maxHp = alivePlayers.Max(p => p.CurrentHealth.Value);
        var topPlayers = alivePlayers.Where(p => p.CurrentHealth.Value == maxHp).ToList();

        if (topPlayers.Count == 1)
            return PlayerDataManager.Instance.GetPlayerNameByIndex(topPlayers[0].PlayerIndex.Value);

        return "Draw";
    }

    private void TriggerFinishSequenceInternal(string winnerName)
    {
        if (isLocalMode)
            FinishAnimationAsync(winnerName).Forget();
        else if (IsServer)
            TriggerFinishSequenceClientRpc(winnerName);
    }

    [ClientRpc]
    private void TriggerFinishSequenceClientRpc(string winnerName)
    {
        FinishAnimationAsync(winnerName).Forget();
    }

    private async UniTaskVoid FinishAnimationAsync(string winnerName)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: this.GetCancellationTokenOnDestroy());
        await CurtainManager.Instance.CloseAsync("Finish!", GetType().Name, 0.1f);

        string resultMessage = (winnerName == "Game Over" || winnerName == "Draw") ? winnerName : $"{winnerName}  Win!";
        gameUIManager.ShowResult(resultMessage);

        await CurtainManager.Instance.OpenAsync(GetType().Name, 0.1f);

        if (isLocalMode || IsServer)
        {
            gameUIManager.ShowEndButton();
            SettingEndButton();
        }
    }

    private void SettingEndButton()
    {
        gameUIManager.endButton.onClick.AddListener(() =>
            GameSceneManager.Instance.LoadNetworkScene(Scene.Title.ToString()));

        gameUIManager.reMatchButton.onClick.AddListener(() =>
            GameSceneManager.Instance.LoadNetworkScene(Scene.ArcanaSelect.ToString()));
    }

    private void UpdateGameStateTextInternal(string text)
    {
        if (isLocalMode) gameUIManager.UpdateGameStateText(text);
        else if (IsServer) UpdateGameStateTextClientRpc(text);
    }

    private void HideGameStateTextInternal()
    {
        if (isLocalMode) gameUIManager.HideGameStateText();
        else if (IsServer) HideGameStateTextClientRpc();
    }

    [ClientRpc]
    private void UpdateGameStateTextClientRpc(string text) => gameUIManager.UpdateGameStateText(text);

    [ClientRpc]
    private void HideGameStateTextClientRpc() => gameUIManager.HideGameStateText();


    public override void OnNetworkDespawn()
    {
        // イベントハンドラの解除（メモリリーク・多重登録防止）
        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        }

    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this) onInitialized.OnCompleted();
        playerSubscriptions.Dispose();
    }
}