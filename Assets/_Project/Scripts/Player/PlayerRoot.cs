using R3;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRoot : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private int maxHealth = 100;
    PlayerStatus status = new PlayerStatus();    //プレイヤーステータス
    [SerializeField] private GameObject magicStart; //予備動作用のオブジェクト


    [Header("Skill & Arcana")]
    public Arcana CurrentArcana;
    public Skill[] SkillList  = new Skill[4];
    public ReactiveProperty<int> SelectedSkillIndex { get; } = new(0);

    [Header("Components")]
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerRayInput rayInput;
    [SerializeField] private PlayerActionController actionController;
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private NetworkPlayer playerSkill;


    [SerializeField] private AimCursor aim;

    // --- プレイヤー情報（ReactivePropertyで管理） ---

    public ReactiveProperty<int> PlayerIndex { get; } = new(-1);
    public ReactiveProperty<int> CurrentHealth { get; } = new(100);
    public ReactiveProperty<int> Nowjump { get; } = new(0);

    public ReactiveProperty<bool> IsDown { get; } = new(false);

    // --- 入力状態（ReactivePropertyで管理） ---
    public ReactiveProperty<bool> IsMove { get; } = new(true);
    public ReactiveProperty<bool> IsJump { get; } = new(false);
    public ReactiveProperty<bool> IsChangeMove { get; } = new(false);
    public ReactiveProperty<bool> IsNormalAttack { get; } = new(false);

    // エフェクト（バフ・デバフ）管理
    public ReactiveProperty<List<EffectAbility>> ActiveEffects { get; } = new(new List<EffectAbility>());

    private IPlayerActionHandler actionHandler;


    [SerializeField] bool previewInput = false;

    /// <summary>
    /// コントローラー（オフライン/オンライン）から呼ばれる初期化処理
    /// </summary>
    public void Initialize(IPlayerActionHandler handler)
    {
        actionHandler = handler;         // IPlayerActionHandler を保持


        CurrentArcana = PlayerDataManager.Instance.GetMyArcana();
        SkillList = PlayerDataManager.Instance.GetMySkills();

        movement.Initialize(this);       // PlayerMovement に PlayerRoot を渡す
        actionController.Initialize();   // PlayerActionController の初期化
        CurrentHealth.Value = maxHealth; // 初期体力を設定

        playerSkill.Initialize(this,false);

        GameUIManager.Instance. BindPlayerStatus(PlayerIndex.Value, ActiveEffects);

        if (TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
        {
            // オンライン時はオーナー（自分）のときだけ、オフライン時は常に実行
            if (netObj.IsOwner || PlayerDataManager.Instance.IsLocalMode)
            {
                if (PlayerUIManager.Instance != null && PlayerDataManager.Instance != null)
                {
                    PlayerUIManager.Instance.Initialize(PlayerDataManager.Instance);
                }
            }
        }
        else
        {
            // そもそも NetworkObject がない純粋なローカル環境の場合
            if (PlayerUIManager.Instance != null && PlayerDataManager.Instance != null)
            {
                PlayerUIManager.Instance.Initialize(PlayerDataManager.Instance);
            }
        }

        if (playerAnimator != null)
        {
            playerAnimator.Initialize(this, inputController, PlayerIndex.Value, actionHandler);
        }
        // --- 入力ストリームの購読 ---

        // 移動
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

        // ジャンプ
        inputController.OnJumpAsObservable
            .Where(_ => actionHandler != null && actionHandler.CanProcessInput && IsJump.Value)
            .Subscribe(_ => movement.ExecuteJump()).AddTo(this);

        // 攻撃
        inputController.OnAttackAsObservable
            .Where(_ => actionHandler != null && actionHandler.CanProcessInput && IsNormalAttack.Value)
            .Subscribe(_ => actionHandler.RequestAttack()).AddTo(this);

        // スキル選択
        inputController.OnSkillSelectAsObservable
            .Where(_ => actionHandler != null && actionHandler.CanProcessInput)
            .Subscribe(dir => {
                //actionHandler.RequestSkillSelect(dir);
                playerSkill.ChangeSelectedSkill(dir);
            }).AddTo(this);

        // スキル使用
        inputController.OnSkillUseAsObservable
            .Where(_ => actionHandler != null && actionHandler.CanProcessInput)
            .Subscribe(_ => actionHandler.RequestSkillUse()).AddTo(this);

        // エフェクト管理
        ActiveEffects.Subscribe(effects =>
        {
            
        }).AddTo(this);
    }

    public void Update()
    {
        if (actionHandler == null) return;

        previewInput = actionHandler.CanProcessInput;

        if (!actionHandler.CanProcessInput) return;

        playerSkill.SkillUpdate();
        movement.UpdateMovement();
        IsJump.Value = rayInput.IsGrounded(movement.GetRigidbody());

    }

    public void LateUpdate()
    {
        if (actionHandler == null || !actionHandler.CanProcessInput) return;
        actionController.LateUpdateAim();
    }

    /// <summary>
    /// サーバーまたはオフライン時に呼ばれる純粋なダメージ計算処理
    /// </summary>
    public void ApplyDamage(int damage)
    {
        if (IsDown.Value) return;

        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value - damage, 0, maxHealth);

        if (CurrentHealth.Value <= 0)
        {
            IsDown.Value = true;
            IsMove.Value = false; // 死亡時は移動不可にするなど
        }
    }

    public void ApplyHeal(int healAmount)
    {
        if (IsDown.Value) return;
        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value + healAmount, 0, maxHealth);
    }

    // --- ロジックの移植 ---
    public void AddEffect(EffectAbility effect)
    {
        var currentList = ActiveEffects.Value;
        currentList.Add(effect);

        // UI側へ通知を飛ばすために再代入（SetValueAndForceNotifyなどを利用しても良いです）
        ActiveEffects.Value = new List<EffectAbility>(currentList);
    }


    public float GetMoveSpeed() => moveSpeed;
    public float GetJumpForce() => jumpForce;
    public PlayerActionController GetActionController() => actionController;
    public AimCursor GetAimCursor() { return aim; }

    public GameObject GetMagic() { return magicStart; }
    public PlayerStatus GetPlayerStatus() => status;

    /// <summary>
    /// リセット用のメソッド
    /// </summary>
    public void ResetToInitialState()
    {
        // ステータスを新品に入れ替える
        status = new PlayerStatus();
        //NULLだったら愚者（逆）を入れる
        if (CurrentArcana == null)
            CurrentArcana = LoadManager.Instance.GetData(0, false);

        ActiveEffects.Value.Clear();

        CurrentHealth.Value = (int)status.GetMaxHp();
    }

    /**
 * --------- セッター ---------
 */
    public void SetMoveSpeed(float speed) { this.moveSpeed = speed; }
    public void SetIsMove(bool isMove)
    {
        this.IsMove.Value = isMove;
    }
    public void SetIsJump(bool isJump) { this.IsJump.Value = isJump; }

    public void SetIsChangeMove(bool isChangeMove) { this.IsChangeMove.Value = isChangeMove; }
    public void SetIsNormalAttack(bool isNormalAttack) { this.IsNormalAttack.Value = isNormalAttack; }

}