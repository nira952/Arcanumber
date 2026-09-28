using R3;
using Unity.Netcode;
using UnityEngine;

public class TimeManager : NetworkBehaviour
{
    [Header("Mode Settings")]
    private bool isLocalMode = false; // インスペクターまたは初期化時に切り替え

    [Header("Timer Settings")]
    [Tooltip("制限時間（秒単位）。5分 = 300秒")]
    [SerializeField] private float maxTimeInSeconds = 300f;

    // サーバーのみが書き込み可能、全員が読み取り可能な NetworkVariable
    public NetworkVariable<float> NetworkTime = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // クライアント/ローカルが依存する ReactiveProperty
    public ReactiveProperty<float> RemainingTime { get; } = new(0f);
    public Subject<Unit> onTimeUp = new Subject<Unit>();

    private float timerAccumulator;
    private bool isTimerRunning = false;
    private bool hasTimeUpTriggered = false;


    private void Awake()
    {
        // ローカルモードかどうかを判定
        isLocalMode = PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsLocalMode;
    }


    // ローカルモードのスタートポイント
    private void Start()
    {
        if (!isLocalMode) { return; }

        // ローカルモード時の初期化処理
        SetRemainingTime(Mathf.CeilToInt(maxTimeInSeconds));
    }

    // ネットワークモードのスタートポイント
    public override void OnNetworkSpawn()
    {
        if (isLocalMode) return;

        // 初期値を ReactiveProperty に同期
        RemainingTime.Value = NetworkTime.Value;

        // NetworkVariable の値が変化した時に ReactiveProperty を更新する
        NetworkTime.AsObservable().Subscribe(newValue =>RemainingTime.Value = newValue).AddTo(this);

        // サーバー側でのみ初期時間をセット
        if (IsServer)
        {
            NetworkTime.Value = Mathf.CeilToInt(maxTimeInSeconds);
        }
    }


    public void Initialize(GameManager gameManager)
    {
        // ゲームの状態に応じてタイマーの稼働を制御
        gameManager.StateRx.Subscribe(state =>
        {
            isTimerRunning = (state == GameState.Playing);
        }).AddTo(this);
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        // 処理を実行すべき権限があるか判定（ローカルモード OR オンラインのサーバー）
        bool canUpdateTimer = isLocalMode || (IsSpawned && IsServer);
        if (!canUpdateTimer) return;

        float currentRemaining = RemainingTime.Value;

        if (currentRemaining > 0)
        {
            timerAccumulator += Time.deltaTime;

            if (timerAccumulator >= 1.0f)
            {
                float newTime = Mathf.Max(0, currentRemaining - 1f);
                SetRemainingTime(newTime);
                timerAccumulator -= 1.0f;
            }
        }
    }

    /// <summary>
    /// モードに応じて適切に残り時間を更新するヘルパーメソッド
    /// </summary>
    private void SetRemainingTime(float newTime)
    {
        if (isLocalMode)
        {
            // ローカルモード：直接 ReactiveProperty を更新し、タイムアップ判定も行う
            float previous = RemainingTime.Value;
            RemainingTime.Value = newTime;
            CheckTimeUp(previous, newTime);
        }
        else if (IsServer)
        {
            // オンラインモード（サーバー）：NetworkVariable を更新
            // ※ NetworkTime の OnValueChanged を通じて全クライアントの RemainingTime と CheckTimeUp が実行されます
            NetworkTime.Value = newTime;
        }
    }

    // NetworkVariable の値が変わった時に各クライアントで呼ばれるコールバック
    private void OnNetworkTimeChanged(float previousValue, float newValue)
    {
        RemainingTime.Value = newValue;
        CheckTimeUp(previousValue, newValue);
    }

    // タイムアップの検知（共通ロジック）
    private void CheckTimeUp(float previousValue, float newValue)
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