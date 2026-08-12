using Cysharp.Threading.Tasks;
using NPOI.SS.Formula.Functions;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace nira.Demo
{
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
        private bool isLocalMode = false; // 追加：オフラインデバッグモード

        #region Singleton Pattern
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


        // --- ネットワーク変数 ---
        public NetworkVariable<GameState> CurrentState = new NetworkVariable<GameState>(GameState.Initialize);
        private readonly ReactiveProperty<GameState> stateRx = new ReactiveProperty<GameState>(GameState.Initialize);
        public ReadOnlyReactiveProperty<GameState> StateRx => stateRx;


        // 全端末で自動同期されるNetworkListを定義
        private readonly NetworkList<NetworkObjectReference> playerNetworkList = new();

        // クライアント側でも扱いやすいように PlayerRoot のリストを返すプロパティ
        [SerializeField] private List<PlayerRoot> localPlayerCache = new();
        public IReadOnlyList<PlayerRoot> Players => isLocalMode ? localPlayerCache : GetPlayersFromNetworkList();

        // キャンセル管理用（GameObject 破棄時に自動で Dispose される）
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private void Awake()
        {
            SetUpSingleton(); // シングルトンの初期化

            // デバッグモード（オフライン）かどうかを判定
            isLocalMode = PlayerDataManager.Instance.IsLocalMode;

            timeManager.Initialize(this);

            timeManager.onTimeUp.Subscribe(_ =>
            {
                // タイムアップ時の処理をメソッドに切り出し
                HandleTimeUp();
            }).AddTo(this);
        }

        private void Start()
        {
            // デバッグモード（オフライン）の場合、OnNetworkSpawnが呼ばれないためここで初期化・監視を行う
            if (isLocalMode)
            {
                Debug.Log("[DebugMode] GameManager オフラインモードで起動します。");

                // ステート変更時の処理を直接購読
                stateRx.Subscribe(state =>
                {
                    Debug.Log($"[GameManager Offline] State changed to: {state}");
                    HandleStateChangeUI(state);
                }).AddTo(this);
            }
            CurtainManager.Instance.FullOpenAsync(GetType().Name).Forget();

        }

        public override void OnNetworkSpawn()
        {
            playerNetworkList.OnListChanged += OnPlayerListChanged;

            CurrentState.OnValueChanged += (previousValue, newValue) =>
            {
                stateRx.Value = newValue;
                Debug.Log($"Game State changed to: {newValue}");
                HandleStateChangeUI(newValue);
            };


            // NetworkVariable の OnValueChanged イベントを Observable（ストリーム）に変換
            Observable.FromEvent<NetworkVariable<int>.OnValueChangedDelegate, int>(
                h => (prev, curr) => h(curr), // イベントハンドラを R3 の Action<int> に変換
                h => timeManager.RemainingTime.OnValueChanged += h,
                h => timeManager.RemainingTime.OnValueChanged -= h
            )
            // 初期値（スポーン時点の RemainingTime.Value）も最初に一発流す
            .Prepend(timeManager.RemainingTime.Value)
            // UI 更新処理を実行
            .Subscribe(seconds => gameUIManager.UpdateTimerDisplay(seconds))
            .AddTo(disposables); // 自動破棄の登録

            // ステートが切り替わった時、ログを流す
            stateRx.Subscribe(state =>
            {
                Debug.Log($"[GameManager] State changed to: {state}");
            }).AddTo(this);
        }

        public override void OnNetworkDespawn()
        {
            playerNetworkList.OnListChanged -= OnPlayerListChanged;
        }

        /// <summary>
        /// ネットワークリストの変更を全端末で検知し、キャッシュを最新化する
        /// </summary>
        private void OnPlayerListChanged(NetworkListEvent<NetworkObjectReference> changeEvent)
        {
            localPlayerCache = GetPlayersFromNetworkList();
            Debug.Log($"[GameManager] プレイヤーリスト更新。現在の参加者数: {localPlayerCache.Count}");
        }
        /// <summary>
        /// オンライン/オフライン両方でUIを更新するための共通メソッド
        /// </summary>
        private void HandleStateChangeUI(GameState state)
        {
            switch (state)
            {
                case GameState.Start:
                    break;
                case GameState.Playing:
                    GameUIManager.Instance.UpdateGameStateText("Start!");
                    break;
                case GameState.Finish:
                    GameUIManager.Instance.UpdateGameStateText("Finish");
                    break;
            }
        }

        /// <summary>
        /// 状態変更用のラッパーメソッド（オフライン時は NetworkVariable を使わない）
        /// </summary>
        private void ChangeGameState(GameState newState)
        {
            if (isLocalMode)
            {
                stateRx.Value = newState;
            }
            else if (IsServer) // クライアントが誤って呼び出しても無視するようにガード
            {
                CurrentState.Value = newState;
            }
        }



        /// <summary>
        /// NetworkList の参照から PlayerRoot のリストを復元するヘルパー
        /// </summary>
        private List<PlayerRoot> GetPlayersFromNetworkList()
        {
            List<PlayerRoot> list = new List<PlayerRoot>();
            foreach (var netRef in playerNetworkList)
            {
                if (netRef.TryGet(out NetworkObject netObj))
                {
                    if (netObj.TryGetComponent<PlayerRoot>(out var player))
                    {
                        list.Add(player);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// プレイヤーを登録する（サーバー専用）
        /// </summary>
        public void RegisterPlayer(PlayerRoot player)
        {
            // オフライン（ローカル）モード時の処理
            if (isLocalMode)
            {
                if (!localPlayerCache.Contains(player))
                {
                    localPlayerCache.Add(player);
                }

                // 💡 ローカルモード時：自分のUIを初期化
                if (PlayerUIManager.Instance != null && PlayerDataManager.Instance != null)
                {
                    PlayerUIManager.Instance.Initialize(PlayerDataManager.Instance);
                }

                if (localPlayerCache.Count >= 1)
                {
                    StartGameSequenceAsync().Forget();
                }
                return;
            }

            // オンライン時：サーバー側ガード
            if (!IsServer) return;

            if (player.TryGetComponent<NetworkObject>(out var netObj))
            {
                // 重複チェック：NetworkObjectId（ulong）で直接判定する
                bool alreadyRegistered = false;
                foreach (var existingRef in playerNetworkList)
                {
                    // NetworkObjectReference から NetworkObjectId を安全に取得して比較
                    if (existingRef.TryGet(out NetworkObject existingObj) && existingObj.NetworkObjectId == netObj.NetworkObjectId)
                    {
                        alreadyRegistered = true;
                        break;
                    }
                }

                if (!alreadyRegistered)
                {
                    // 明示的に NetworkObjectReference を作成して追加
                    playerNetworkList.Add(new NetworkObjectReference(netObj));
                }

                // 💡 オンライン時：登録されたタイミングで自分のUIを初期化
                // （各クライアントが自分の画面でこの処理を通る、またはローカルプレイヤーの判定が必要な場合は `netObj.IsOwner` を使います）
                if (netObj.IsOwner)
                {
                    if (PlayerUIManager.Instance != null && PlayerDataManager.Instance != null)
                    {
                        PlayerUIManager.Instance.Initialize(PlayerDataManager.Instance);
                    }
                }

                int totalPlayers = PlayerDataManager.Instance.GetLobbyPlayerCount();

                Debug.Log($"[GameManager] サーバー登録完了: {player.name} (ID: {netObj.NetworkObjectId}). 現在の参加者数: {playerNetworkList.Count}/{totalPlayers}");

                // 全員揃ったらゲーム開始シーケンスを開始（サーバー側で判定・実行）
                if (playerNetworkList.Count >= totalPlayers)
                {
                    // PlayerUtilityに全プレイヤーの参照を渡す
                    foreach (var root in Players)
                    {
                        PlayerUtility.RegisterPlayer(root.gameObject.GetComponent<NetworkPlayer>());
                    }
                    StartGameSequenceAsync().Forget();
                }
            }
        }
        
        /// <summary>
        /// 自分以外の全プレイヤーを取得（クライアント側からも正常に呼べます）
        /// </summary>
        public List<PlayerRoot> GetOtherPlayers(PlayerRoot self)
        {
            return Players.Where(p => p != self).ToList();
        }
        // ====================================================================
        // ゲーム開始シーケンス & 同期処理
        // ====================================================================

        private async UniTaskVoid StartGameSequenceAsync()
        {
            ChangeGameState(GameState.Ready);

            // 3, 2, 1 カウントダウン
            for (int i = 3; i > 0; i--)
            {
                UpdateGameStateTextInternal(i.ToString()); // 変更後
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            ChangeGameState(GameState.Start);
            await UniTask.Yield(PlayerLoopTiming.Update);
            ChangeGameState(GameState.Playing);

            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: this.GetCancellationTokenOnDestroy());

            HideGameStateTextInternal(); // 変更後
        }

        /// <summary>
        /// 全端末（オフライン時は自端末のみ）でUIテキストを更新する
        /// </summary>
        private void UpdateGameStateTextInternal(string text)
        {
            if (isLocalMode)
            {
                gameUIManager.UpdateGameStateText(text);
            }
            else if (IsServer)
            {
                UpdateGameStateTextClientRpc(text);
            }
        }

        /// <summary>
        /// 全端末でUIテキストを非表示にする
        /// </summary>
        private void HideGameStateTextInternal()
        {
            if (isLocalMode)
            {
                gameUIManager.HideGameStateText();
            }
            else if (IsServer)
            {
                HideGameStateTextClientRpc();
            }
        }


        [ClientRpc]
        private void UpdateGameStateTextClientRpc(string text)
        {
            gameUIManager.UpdateGameStateText(text);
        }

        [ClientRpc]
        private void HideGameStateTextClientRpc()
        {
            gameUIManager.HideGameStateText();
        }

        // ====================================================================
        // タイムアップ時の勝敗判定処理
        // ====================================================================
        private void HandleTimeUp()
        {
            // 勝敗判定と演出のトリガーはサーバー（またはオフラインホスト）のみで行う
            if (!isLocalMode && !IsServer) return;

            // ゲームステートをFinishに変更し、操作等を止める
            ChangeGameState(GameState.Finish);

            IReadOnlyList<PlayerRoot> currentPlayers = Players;
            if (currentPlayers == null || currentPlayers.Count == 0) return;

            // 生きている（ダウンしていない）プレイヤーを抽出
            var alivePlayers = currentPlayers.Where(p => p != null && !p.IsDown.Value).ToList();
            string winnerName = "Draw";

            if (alivePlayers.Count == 0)
            {
                // 全滅している場合
                winnerName = isLocalMode ? "Game Over" : "Draw";
            }
            else if (alivePlayers.Count == 1)
            {
                // 1人のみ生存している場合
                winnerName = PlayerDataManager.Instance.GetPlayerNameByIndex(alivePlayers[0].PlayerIndex.Value);
            }
            else
            {
                // 複数人が生存している場合、最も体力の多いプレイヤーを探す
                // 【注意】 PlayerRoot の体力プロパティ名（例: CurrentHealth.Value）に合わせて以下のプロパティを書き換えてください
                int maxHp = alivePlayers.Max(p => p.CurrentHealth.Value);

                // 最大HPを持つプレイヤーのリストを取得（同値による引き分けを考慮）
                var topPlayers = alivePlayers.Where(p => p.CurrentHealth.Value == maxHp).ToList();

                if (topPlayers.Count == 1)
                {
                    // 単独トップの場合
                    winnerName = PlayerDataManager.Instance.GetPlayerNameByIndex(topPlayers[0].PlayerIndex.Value);
                }
                else
                {
                    // 最大HPが同じプレイヤーが複数いる場合は引き分け
                    winnerName = "Draw";
                }
            }

            // リザルト演出を開始
            TriggerFinishSequenceInternal(winnerName);
        }
        // ====================================================================

        public void CheckFinishCondition()
        {
            if (StateRx.CurrentValue != GameState.Playing) return;
            if (!isLocalMode && !IsServer) return;

            IReadOnlyList<PlayerRoot> currentPlayers = Players;

            if (currentPlayers == null || currentPlayers.Count == 0) return;

            int downCount = currentPlayers.Count(p => p != null && p.IsDown.Value);
            int totalPlayers = isLocalMode ? 1 : Players.Count;

            Debug.Log($"[CheckFinishCondition] DownCount: {downCount} / Total: {totalPlayers}");

            string winnerName = "";

            if (totalPlayers <= 1)
            {
                // 各クライアントのステートを Finish にしてプレイヤーの操作等を止める
                ChangeGameState(GameState.Finish);

                if (downCount >= 1)
                {
                    // オフライン（1人）でダウンした場合
                    winnerName = "Game Over";
                    TriggerFinishSequenceInternal(winnerName);
                }
            }
            else
            {
                // 各クライアントのステートを Finish にしてプレイヤーの操作等を止める
                ChangeGameState(GameState.Finish);

                if (downCount >= totalPlayers - 1)
                {
                    // 生き残っているプレイヤーを探す
                    var winner = currentPlayers.FirstOrDefault(p => p != null && !p.IsDown.Value);
                    if (winner != null)
                    {
                        string playerName = PlayerDataManager.Instance.GetPlayerNameByIndex(winner.PlayerIndex.Value);

                        winnerName = playerName;
                    }
                    else
                    {
                        // 全員同時にダウンした（相打ち）場合
                        winnerName = "Draw";
                    }

                    TriggerFinishSequenceInternal(winnerName);
                }
            }
        }

        // ====================================================================
        // リザルトアニメーションの同期処理
        // ====================================================================

        /// <summary>
        /// オフライン/オンラインに応じてリザルト演出のトリガーを振り分ける
        /// </summary>
        private void TriggerFinishSequenceInternal(string winnerName)
        {
            if (isLocalMode)
            {
                FinishAnimationAsync(winnerName).Forget();
            }
            else if (IsServer)
            {
                TriggerFinishSequenceClientRpc(winnerName);
            }
        }

        /// <summary>
        /// 全クライアントに対してリザルト演出の開始を指示する
        /// </summary>
        [ClientRpc]
        private void TriggerFinishSequenceClientRpc(string winnerName)
        {
            FinishAnimationAsync(winnerName).Forget();
        }

        /// <summary>
        /// 実際のアニメーションとUI表示（各端末のローカルで実行される）
        /// </summary>
        private async UniTaskVoid FinishAnimationAsync(string winnerName)
        {

            // 1秒待ってからゲーム終了処理を行う
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: this.GetCancellationTokenOnDestroy());

            await CurtainManager.Instance.CloseAsync("Finish!", GetType().Name, 0.1f);

            // 表示するメッセージの組み立て
            string resultMessage;
            if (winnerName == "Game Over" || winnerName == "Draw")
            {
                resultMessage = winnerName;
            }
            else
            {
                resultMessage = $"{winnerName}  Win!";
            }

            gameUIManager.ShowResult(resultMessage);

            await CurtainManager.Instance.OpenAsync(GetType().Name, 0.1f);

            if (IsServer)
            {
                gameUIManager.ShowEndButton();
                SettingEndButton();
            }

        }

        private void SettingEndButton()
        {
            gameUIManager.endButton.onClick.AddListener(() =>
            {
                // 終了処理
                GameSceneManager.Instance.LoadNetworkScene(Scene.Title.ToString());
            });

            gameUIManager.reMatchButton.onClick.AddListener(() =>
            {
                // リマッチ処理
                GameSceneManager.Instance.LoadNetworkScene(Scene.ArcanaSelect.ToString());
            });
        }



        public override void OnDestroy()
        {
            base.OnDestroy();
            // 念のためシーン切り替え時などに完了させる
            if (Instance == this) onInitialized.OnCompleted();
        }
    }
}