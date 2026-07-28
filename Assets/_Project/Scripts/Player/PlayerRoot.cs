using UnityEngine;
using R3;

[RequireComponent(typeof(PlayerInputController), typeof(PlayerMovement), typeof(PlayerRayInput))]
public class PlayerRoot : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;

    [Header("Components")]
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerRayInput rayInput;
    [SerializeField] private PlayerActionController actionController;

    // 状態管理
    public ReactiveProperty<bool> IsMove { get; } = new(true);
    public ReactiveProperty<bool> IsJump { get; } = new(false);
    public ReactiveProperty<bool> IsChangeMove { get; } = new(false);
    public ReactiveProperty<bool> IsNormalAttack { get; } = new(false);
    public ReactiveProperty<bool> IsDown { get; } = new(false);

    private IPlayerActionHandler actionHandler;

    /// <summary>
    /// コントローラー（オフライン/オンライン）から呼ばれる初期化処理
    /// </summary>
    public void Initialize(IPlayerActionHandler handler)
    {
        actionHandler = handler;
        movement.Initialize(this);
        actionController.Initialize();

        // --- 入力ストリームの購読 ---
        // actionHandler.CanProcessInput が true の時のみ処理を通す

        // 移動（移動処理はその場で完結するため、インターフェースを通さず直接Movementへ流す）
        inputController.OnMoveAsObservable
            .Where(_ => actionHandler != null && actionHandler.CanProcessInput)
            .Subscribe(rawInput =>
            {
                if (!IsMove.Value) return;
                float finalInput = !IsChangeMove.Value ? rawInput : -rawInput;
                movement.SetMoveInput(finalInput);
            }).AddTo(this);

        IsMove.Where(canMove => !canMove)
              .Subscribe(_ => movement.SetMoveInput(0f)).AddTo(this);

        // ジャンプ（エフェクトや音の同期が必要なため、インターフェースへ委譲）
        inputController.OnJumpAsObservable
            .Where(_ => actionHandler != null && actionHandler.CanProcessInput && IsJump.Value)
            .Subscribe(_ => actionHandler.RequestJump()).AddTo(this);

        // 攻撃
        inputController.OnAttackAsObservable
            .Where(_ => actionHandler != null && actionHandler.CanProcessInput && IsNormalAttack.Value)
            .Subscribe(_ => actionHandler.RequestAttack()).AddTo(this);

        // スキル選択・使用
        inputController.OnSkillSelectAsObservable
            .Where(_ => actionHandler != null && actionHandler.CanProcessInput)
            .Subscribe(dir => actionHandler.RequestSkillSelect(dir)).AddTo(this);

        inputController.OnSkillUseAsObservable
            .Where(_ => actionHandler != null && actionHandler.CanProcessInput)
            .Subscribe(_ => actionHandler.RequestSkillUse()).AddTo(this);
    }

    public void Update()
    {
        // Playing 以外や他人のキャラはここで毎フレーム弾かれるため、移動が停止します
        if (actionHandler == null || !actionHandler.CanProcessInput) return;

        movement.UpdateMovement();
        IsJump.Value = rayInput.IsGrounded(movement.GetRigidbody());
    }

    public void LateUpdate()
    {
        if (actionHandler == null || !actionHandler.CanProcessInput) return;
        actionController.LateUpdateAim();
    }

    public float GetMoveSpeed() => moveSpeed;
    public float GetJumpForce() => jumpForce;
    public PlayerActionController GetActionController() => actionController;
}