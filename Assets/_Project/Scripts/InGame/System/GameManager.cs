using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
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

        // --- ネットワーク変数 ---
        public NetworkVariable<GameState> CurrentState = new NetworkVariable<GameState>(GameState.Initialize);
        public NetworkVariable<int> RemainingTime = new NetworkVariable<int>(0);

        private readonly ReactiveProperty<GameState> stateRx = new ReactiveProperty<GameState>(GameState.Initialize);
        public ReadOnlyReactiveProperty<GameState> StateRx => stateRx;

        [SerializeField] private List<PlayerRoot> players = new List<PlayerRoot>();

        private void Awake()
        {
            SetUpSingleton(); // シングルトンの初期化

            // デバッグモード（オフライン）かどうかを判定
            isLocalMode = PlayerDataManager.Instance.IsLocalMode;
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
        }

        public override void OnNetworkSpawn()
        {
            CurrentState.OnValueChanged += (previousValue, newValue) =>
            {
                stateRx.Value = newValue;
                Debug.Log($"Game State changed to: {newValue}");
                HandleStateChangeUI(newValue);
            };

            RemainingTime.OnValueChanged += (oldVal, newVal) =>
            {
                if (CurrentState.Value == GameState.Ready && newVal > 0)
                {
                    GameUIManager.Instance.UpdateTimer(newVal.ToString());
                }
            };

            // ステートが切り替わった時、ログを流す
            stateRx.Subscribe(state =>
            {
                Debug.Log($"[GameManager] State changed to: {state}");
            }).AddTo(this);
        }

        /// <summary>
        /// オンライン/オフライン両方でUIを更新するための共通メソッド
        /// </summary>
        private void HandleStateChangeUI(GameState state)
        {
            switch (state)
            {
                case GameState.Start:
                case GameState.Playing:
                    GameUIManager.Instance.UpdateTimer("START!");
                    break;
                case GameState.Finish:
                    GameUIManager.Instance.UpdateTimer("FINISH!");
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
                stateRx.Value = newState; // Rxを直接更新してイベントを発火させる
            }
            else
            {
                CurrentState.Value = newState; // NGOの同期機能を使ってイベントを発火させる
            }
        }

        /// <summary>
        /// 残り時間変更用のラッパーメソッド
        /// </summary>
        private void ChangeRemainingTime(int newTime)
        {
            if (isLocalMode)
            {
                // オフライン時は直接UIを更新する
                if (stateRx.Value == GameState.Ready && newTime > 0)
                {
                    GameUIManager.Instance.UpdateTimer(newTime.ToString());
                }
            }
            else
            {
                RemainingTime.Value = newTime; // NGOの同期経由
            }
        }

        /// <summary>
        /// プレイヤーを登録する
        /// </summary>
        /// <param name="player"></param>
        public void RegisterPlayer(PlayerRoot player)
        {
            // デバッグモードなら IsServer 判定を無視する
            if (!isLocalMode && !IsServer) return;

            players.Add(player);

            // オフライン時はプレイヤー1人として扱う
            int totalPlayers = isLocalMode ? 1 : PlayerDataManager.Instance.GetLobbyPlayerCount();

            // 全員が揃ったらゲーム開始シーケンスを開始
            if (players.Count >= totalPlayers)
            {
                StartGameSequenceAsync().Forget();
            }
        }

        private async UniTaskVoid StartGameSequenceAsync()
        {
            // カーテンを開く
            await CurtainManager.Instance.FullOpenAsync(GetType().Name);

            ChangeGameState(GameState.Ready); // ラッパー経由に変更

            for (int i = 3; i > 0; i--)
            {
                ChangeRemainingTime(i); // ラッパー経由に変更
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            ChangeGameState(GameState.Start);
            await UniTask.Yield(PlayerLoopTiming.Update);
            ChangeGameState(GameState.Playing);
        }

        /// <summary>
        /// 【修正版】ダウン数が終了条件を満たしたか判定する
        /// </summary>
        public void CheckFinishCondition()
        {
            // デバッグモードなら IsServer 判定を無視する
            if (!isLocalMode && !IsServer) return;

            // ※注：DemoPlayerの IsDown が NetworkVariable の場合、オフライン時に .Value にアクセスすると
            // エラーになる可能性があります。その場合は DemoPlayer 側にも同様の isDebugMode 対策が必要です。
            int downCount = players.Count(p => p.IsDown.Value);

            // オフライン時は強制的に1人デバッグモードの挙動にする
            int totalPlayers = isLocalMode ? 1 : PlayerDataManager.Instance.GetLobbyPlayerCount();

            if (totalPlayers <= 1) // 安全のため <= 1 に修正
            {
                // 1人デバッグ時：自分がダウン（downCountが1）したら終了
                if (downCount >= 1)
                {
                    ChangeGameState(GameState.Finish);
                }
            }
            else
            {
                // 複数人プレイ時：自分以外の全員（総人数 - 1）がダウンしたら終了
                if (downCount >= totalPlayers - 1)
                {
                    ChangeGameState(GameState.Finish);
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            // 念のためシーン切り替え時などに完了させる
            if (Instance == this) onInitialized.OnCompleted();
        }
    }
}