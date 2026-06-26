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

        public NetworkVariable<GameState> CurrentState = new NetworkVariable<GameState>(GameState.Initialize);
        public NetworkVariable<int> RemainingTime = new NetworkVariable<int>(0);

        private readonly ReactiveProperty<GameState> stateRx = new ReactiveProperty<GameState>(GameState.Initialize);
        public ReadOnlyReactiveProperty<GameState> StateRx => stateRx;

        [SerializeField] private List<DemoPlayer> players = new List<DemoPlayer>();

        private void Awake()
        {
            SetUpSingleton(); // シングルトンの初期化
        }

        public override void OnNetworkSpawn()
        {
            CurrentState.OnValueChanged += (previousValue, newValue) =>
            {
                stateRx.Value = newValue;
                Debug.Log($"Game State changed to: {newValue}");

                switch (newValue)
                {
                    case GameState.Start:
                    case GameState.Playing:
                        GameUIManager.Instance.UpdateTimer("START!");
                        break;
                    case GameState.Finish:
                        GameUIManager.Instance.UpdateTimer("FINISH!");
                        break;
                }
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

        public void RegisterPlayer(DemoPlayer player)
        {
            if (!IsServer) return;

            players.Add(player);
            int totalPlayers = PlayerDataManager.Instance.GetLobbyPlayerCount();

            if (players.Count == totalPlayers)
            {
                StartGameSequenceAsync().Forget();
            }
        }

        private async UniTaskVoid StartGameSequenceAsync()
        {
            CurrentState.Value = GameState.Ready;

            for (int i = 3; i > 0; i--)
            {
                RemainingTime.Value = i;
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            CurrentState.Value = GameState.Start;
            await UniTask.Yield(PlayerLoopTiming.Update);
            CurrentState.Value = GameState.Playing;
        }

        /// <summary>
        /// 【修正版】ダウン数が終了条件を満たしたか判定する
        /// </summary>
        public void CheckFinishCondition()
        {
            if (!IsServer) return;

            int downCount = players.Count(p => p.IsDown.Value);
            int totalPlayers = PlayerDataManager.Instance.GetLobbyPlayerCount();

            if (totalPlayers == 1)
            {
                // 1人デバッグ時：自分がダウン（downCountが1）したら終了
                if (downCount >= 1)
                {
                    CurrentState.Value = GameState.Finish;
                }
            }
            else
            {
                // 複数人プレイ時：自分以外の全員（総人数 - 1）がダウンしたら終了
                if (downCount >= totalPlayers - 1)
                {
                    CurrentState.Value = GameState.Finish;
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