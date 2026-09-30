using R3;
using Unity.Netcode;
using UnityEngine;

public class TimeManager : NetworkBehaviour
{
    [Header("Mode Settings")]
    private bool isLocalMode = false;

    [Header("Timer Settings")]
    [Tooltip("制限時間（秒単位）。5分 = 300秒")]
    [SerializeField] private int maxTimeInSeconds = 300;

    // 1秒ごとに整数（int）のみを同期（通信量を最小化）
    public NetworkVariable<int> NetworkTime = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // クライアント/UIが参照する ReactiveProperty（int型に統一）
    public ReactiveProperty<int> RemainingTime { get; } = new(0);
    public Subject<Unit> onTimeUp = new Subject<Unit>();

    private float timerAccumulator;
    private bool isTimerRunning = false;
    private bool hasTimeUpTriggered = false;

    private void Awake()
    {
        isLocalMode = PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsLocalMode;
    }

    private void Start()
    {
        if (!isLocalMode) return;

        // ローカルモード時の初期化
        SetRemainingTime(maxTimeInSeconds);
    }

    public override void OnNetworkSpawn()
    {
        if (isLocalMode) return;

        // 初期値を適用
        RemainingTime.Value = NetworkTime.Value;

        // サーバーからの同期を受けた際に実行するコールバックを正しく登録
        NetworkTime.AsObservable()
            .Subscribe(newValue =>
            {
                int previous = RemainingTime.Value;
                OnNetworkTimeChanged(previous, newValue);
            })
            .AddTo(this);

        // サーバー側でのみ初期時間をセット
        if (IsServer)
        {
            NetworkTime.Value = maxTimeInSeconds;
        }
    }

    public void Initialize(GameManager gameManager)
    {
        gameManager.StateRx.Subscribe(state =>
        {
            isTimerRunning = (state == GameState.Playing);
        }).AddTo(this);
    }

    private void Update()
    {
        if (!IsServer) { return; }

        if (!isTimerRunning) return;

        // タイマー更新権限の確認（ローカル または サーバーのみ）
        bool canUpdateTimer = isLocalMode || (IsSpawned && IsServer);
        if (!canUpdateTimer) return;

        int currentRemaining = RemainingTime.Value;

        if (currentRemaining > 0)
        {
            timerAccumulator += Time.deltaTime;

            // 1秒経過したタイミングで残り秒数を1秒減らす
            if (timerAccumulator >= 1.0f)
            {
                int newTime = Mathf.Max(0, currentRemaining - 1);
                SetRemainingTime(newTime);

                // 1.0f を引いてあまり（誤差）を保持
                timerAccumulator -= 1.0f;
            }
        }
    }

    /// <summary>
    /// 秒数の更新と通信（1秒に1回のみ実行される）
    /// </summary>
    private void SetRemainingTime(int newTime)
    {
        if (isLocalMode)
        {
            int previous = RemainingTime.Value;
            RemainingTime.Value = newTime;
            CheckTimeUp(previous, newTime);
        }
        else if (IsServer)
        {
            // サーバー側で整数値を更新 -> NGOが自動的に1秒周期でクライアントへ通信
            NetworkTime.Value = newTime;
        }
    }

    private void OnNetworkTimeChanged(int previousValue, int newValue)
    {
        RemainingTime.Value = newValue;
        CheckTimeUp(previousValue, newValue);
    }

    private void CheckTimeUp(int previousValue, int newValue)
    {
        if (newValue > 0)
        {
            hasTimeUpTriggered = false;
        }

        if (previousValue > 0 && newValue <= 0 && !hasTimeUpTriggered)
        {
            hasTimeUpTriggered = true;
            OnTimeUp();
        }
    }

    private void OnTimeUp()
    {
        Debug.Log($"[{(isLocalMode ? "Local" : IsServer ? "Server" : "Client")}] タイムアップ！");
        onTimeUp.OnNext(Unit.Default);
    }
}