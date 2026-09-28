using ObservableCollections;
using R3;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRoot : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float jumpForce = 5f;
    private PlayerStatus status = new PlayerStatus();
    [SerializeField] private SpriteRenderer magicStart;

    [Header("Skill & Arcana")]
    private Arcana CurrentArcana;
    private Skill CurrentSkill;

    private List<Skill> skills = new List<Skill>();

    [Header("Components")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerRayInput rayInput;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerActionController actionController;
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private AimCursor aim;
    [SerializeField] private NetworkPlayer playerSkill;

    // --- プレイヤー情報・入力状態 (省略せずにそのまま使用) ---
    public ReactiveProperty<int> PlayerIndex { get; } = new(-1);
    public ReactiveProperty<float> CurrentHealth { get; } = new(100);
    public ReactiveProperty<int> Nowjump { get; } = new(0);
    public ReactiveProperty<bool> IsDown { get; } = new(false);
    public ReactiveProperty<bool> IsMove { get; } = new(true);
    public ReactiveProperty<bool> IsJump { get; } = new(false);
    public ObservableList<EffectAbility> ActiveEffects { get; } = new();

    public bool CanControl => !IsDown.Value && (GameManager.Instance == null || GameManager.Instance.NetWorkGameState.Value == GameState.Playing);

    public void Initialize()
    {
        //　ArcanaとスキルをPlayerDataManagerから取得
        Skill[] skillList = PlayerDataManager.Instance.GetMySkills();
        CurrentSkill = skillList[0];
        CurrentArcana = PlayerDataManager.Instance.GetMyArcana();

        // ArcanaがNULLだったら愚者（逆）を入れる
        if (CurrentArcana == null)
            CurrentArcana = LoadManager.Instance.GetData(0, false);

        // 初期発動のアルカナを発動する
        CurrentArcana.ExecuteArcanaEffect(ASkillCategory.StartEffect, playerSkill);

        // 最大体力を設定
        CurrentHealth.Value = status.GetMaxHp();

        // エフェクトの初期化
        ActiveEffects.Clear();

        // ステータスを新品に入れ替える
        status = new PlayerStatus();

        // コンポーネントの初期化
        movement.Initialize(this);
        actionController.Initialize();
        playerAttack.Initialized(this);
        playerAnimator.Initialize(PlayerIndex.Value);
        playerSkill.Initialize(this, aim, skillList);
    }


    public void Update()
    {
        // 操作できない場合は移動を停止して処理を終了
        if (!CanControl) { movement.StopMovement(); return; }

        if (IsDown.Value) { movement.StopMovement(); return; }

        if(HaveEffect(EffectList.Stun, true)) { movement.StopMovement(); return; }

        // スキルのクールタイムを更新
        playerSkill.UpdateAllCoolTimes();

        // 移動処理の更新
        movement.UpdateMovement();

        // ジャンプ状態の更新
        IsJump.Value = rayInput.IsGrounded(movement.GetRigidbody());


        // 状態異常の更新処理
        for (int i = ActiveEffects.Count - 1; i >= 0; i--)
        {
            // nullチェック
            if (ActiveEffects[i] == null) { ActiveEffects.RemoveAt(i); continue; }

            var effect = ActiveEffects[i];

            // 1. 継続時間を減らす
            effect.DecreaseTime(Time.deltaTime);

            // 2. 毒・回復等の Tick 処理を実行
            effect.ExecuteTick(Time.deltaTime, this);

            // 3. 期限切れなら削除
            if (effect.IsExpired)
            {
                ActiveEffects.RemoveAt(i);
            }
        }
    }

    public void LateUpdate()
    {
        if (!CanControl) return;
        actionController.LateUpdateAim();
    }

    // --- ActionHandlerから統合したメソッド群 ---

    public void ExecuteAttack()
    {
        // TODO : 後でNetWorkPlayerに移植する

        //const int ATTACK_ACTION_INDEX = 0;
        //if (!IsActionReady(ATTACK_ACTION_INDEX)) return;

        //// CT開始
        //StartActionCoolTime(ATTACK_ACTION_INDEX, 0.5f); // ※本来の攻撃CTを取得して入れてください

        // エンペラーオーラ状態なら攻撃不可
        if (HaveEffect(EffectList.EnperorAura, true)) { return; }

        playerAttack.ResetList();
        playerAttack.NormalAttackActive();
        CurrentArcana?.ExecuteArcanaEffect(ASkillCategory.SkillEffect,playerSkill);
    }

    /// <summary> 移動実行メソッド </summary>
    public void ExecuteMove(float rawInput)
    {
        if (!IsMove.Value) return;

        // 移動方向の反転処理
        bool isChangeMove = HaveEffect(EffectList.Reverse, true);

        float finalInput = !isChangeMove ? rawInput : -rawInput;

        movement.SetMoveDirection(finalInput);

        // 向きの反転
        playerAnimator.Flip(rawInput);
        playerAttack.Flip(rawInput);

        // ダッシュアニメーションの切り替え
        bool isDashing = Mathf.Abs(rawInput) > 0.01f;
        playerAnimator.SetDash(isDashing);

    }

    /// <summary> ジャンプ実行メソッド </summary>
    public void ExecuteJump()
    {
        // ジャンプ禁止状態ならジャンプ不可
        if (HaveEffect(EffectList.NoJump, true)) { return; }

        movement.JumpActive();
    }

    /// <summary> スキル選択実行メソッド </summary>
    public void ExecuteSkillSelect(int direction)
    {
        playerSkill.SkillSelect(direction);
    }

    // / <summary> スキル使用実行メソッド </summary>
    public void ExecuteSkillUse()
    {
        // サイレンス状態ならスキル使用不可
        if (HaveEffect( EffectList.Silence, false)) { return; }

        playerSkill.SkillUse(CurrentArcana,CurrentSkill);

        // アニメーション再生
        playerAnimator.PlayMagicAnimation();
    }

    /// <summary>
    /// サーバーまたはオフライン時に呼ばれる純粋なダメージ計算処理
    /// </summary>
    public void ApplyDamage(float damage)
    {
        if (IsDown.Value) return;

        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value - damage, 0, status.GetMaxHp());

        if (CurrentHealth.Value <= 0)
        {
            IsDown.Value = true;
            IsMove.Value = false; // 死亡時は移動不可にするなど
        }
    }

    public void ApplyHeal(float healAmount)
    {
        if (IsDown.Value) return;
        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value + healAmount, 0, status.GetMaxHp());
    }

    // --- ロジックの移植 ---
    public void AddEffect(EffectAbility effect)
    {
        ActiveEffects.Add(effect);
    }


    /// <summary>
    /// 状態異常の有無を取得するメソッド
    /// </summary>
    /// <param name="effect"> エフェクトの種類 </param>
    /// <param name="isUp"> 上昇か下降か </param>
    public bool HaveEffect( EffectList effect, bool isUp)
    {
        return ActiveEffects.Any(e => e?.GetEffect() != null &&
                                                 e.GetEffect().GetEffectList() == effect &&
                                                 e.GetEffect().GetIsUp() == isUp);
    }

    public float GetMoveSpeed() => status.GetSpeed();
    public float GetJumpForce() => jumpForce;

    // 現在の攻撃力を取得するメソッド
    public float GetCurrentAttackPower()
    {
        float baseAttackPower = status.GetAtk();
        float attackPowerMultiplier = GetEffectValue(EffectList.ATK); // 攻撃力上昇の効果を取得
        return Mathf.Max(0.1f, baseAttackPower * attackPowerMultiplier);
    }

    // 現在の防御力を取得するメソッド
    public float GetCurrentDefense()
    {
        float baseDefense = status.GetDef();
        float defenseMultiplier = GetEffectValue(EffectList.DEF); // 防御力上昇の効果を取得
        return Mathf.Max(0.1f, baseDefense * defenseMultiplier);
    }

    public PlayerActionController GetActionController() => actionController;
    public AimCursor GetAimCursor() { return aim; }

    public List<EffectAbility> GetEffectList() { return ActiveEffects.ToList(); }

    // リスト内に特定のエフェクトが存在するかを確認するメソッド
    public bool HasEffect(EffectList effect)
    {
        return ActiveEffects.Any(e => e?.GetEffect() != null && e.GetEffect().GetEffectList() == effect);
    }

    public bool HasEffect(EffectList effect, bool isUp)
    {
        foreach (EffectAbility e in ActiveEffects)
        {
            if (e == null || e.GetEffect() == null) continue;
            if (e.GetEffect().GetEffectList() == effect && e.GetEffect().GetIsUp() == isUp)
                return true;
        }
        return false;
    }

    public SpriteRenderer GetMagic() { return magicStart; }
    public PlayerStatus GetPlayerStatus() => status;
    public Arcana GetArcana() { return CurrentArcana; }
    public Skill GetCurrentSkill() { return CurrentSkill; }
    public List<Skill> GetSkill() { return skills; }
    /// <summary>
    /// 指定したプレイヤーが持つ特定の効果の値を取得する。
    /// </summary>
    public float GetEffectValue(EffectList effect)
    {
        float value = 1f;

        // プレイヤーが持つすべての効果を確認し、指定された効果の値を合計する
        foreach (var e in ActiveEffects)
        {
            // nullならスキップ
            if (e?.GetEffect() == null) { continue; }

            // 指定された効果と一致する場合、値を加算または減算する
            if (e.GetEffect().GetEffectList() == effect)
            {
                value += e.GetEffect().GetIsUp() ? e.GetValue() : -e.GetValue();
            }
        }
        // マイナス値を防ぐため、0以上の値を返す
        return Mathf.Max(0f, value);
    }


    /**
 * --------- セッター ---------
 */
    public void SetMoveSpeed(float speed) { status.SetSpeed(speed); }

    public void SetSkills(Skill[] skills)
    {
        this.skills.Clear();
        this.skills.AddRange(skills);
    }

    public void SetArcana(Arcana arcana)
    {
        CurrentArcana = arcana;
    }
}
