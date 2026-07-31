using nira.Demo;
using Unity.Netcode;
using UnityEngine;
using R3;

public class TimeManager : NetworkBehaviour
{

    [Header("Timer Settings")]
    [Tooltip("制限時間（秒単位）。5分 = 300秒")]
    [SerializeField] private float maxTimeInSeconds = 300f;

    // ネットワーク変数（全員に同期される秒数）
    public NetworkVariable<int> RemainingTime = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public ReactiveProperty<bool> isTimeUp = new ReactiveProperty<bool>(false);

    private GameManager gameManager;

    private float timerAccumulator;
    private bool isTimerRunning = false;


    public void Initialize(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    private void Update()
    {
        // ゲームがプレイ中でない場合はタイマーを更新しない
        if (gameManager.CurrentState.Value != GameState.Playing) { return; }

        // 時間経過の処理はサーバー（ホスト）でのみ行う
        if (!IsServer || !isTimerRunning) return;

        if (RemainingTime.Value > 0)
        {
            timerAccumulator += Time.deltaTime;

            // 1秒経つごとに NetworkVariable を減らす
            if (timerAccumulator >= 1.0f)
            {
                RemainingTime.Value = Mathf.Max(0, RemainingTime.Value - 1);
                timerAccumulator -= 1.0f;
            }
        }
        else
        {
            // タイムアップ処理
            isTimerRunning = false;
            OnTimeUp();
        }
    }



    // 外部からタイマーを開始したい場合（ゲーム開始時など）
    public void StartTimer()
    {
        if (IsServer)
        {
            RemainingTime.Value = Mathf.CeilToInt(maxTimeInSeconds);
            isTimerRunning = true;
        }
    }

    // タイムアップ時の処理
    private void OnTimeUp()
    {
        Debug.Log("タイムアップ！");
        // ここにゲーム終了ロジックなどを記述（サーバー主導）
    }
}