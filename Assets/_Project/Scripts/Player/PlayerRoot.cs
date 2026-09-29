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
    private Skill[] skillList = new Skill[GameConfig.SKILL_HOPPER_MAX];

    [Header("Components")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerRayInput rayInput;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private AimCursor aimCursor;
    [SerializeField] private PlayerSkiller playerSkill;

    // --- プレイヤー情報・入力状態 (省略せずにそのまま使用) ---
    public ReactiveProperty<int> PlayerIndex { get; } = new(-1);
    public ReactiveProperty<float> CurrentHealth { get; } = new(100);
    public ReactiveProperty<bool> IsDown { get; } = new(false);
    public ObservableList<EffectAbility> ActiveEffects { get; } = new();
    public ReactiveProperty<int> SelectedSkillIndex { get; } = new(0);

    /// <summary> 入力を受け付けて制御できるかどうかを示します </summary>
    public bool CanControl => 
        !IsDown.Value && (GameManager.Instance == null || GameManager.Instance.StateRx.CurrentValue == GameState.Playing);

    public void Initialize()
    {
        //　ArcanaとスキルをPlayerDataManagerから取得
        skillList = PlayerDataManager.Instance.GetMySkills();
        CurrentArcana = PlayerDataManager.Instance.GetMyArcana();
        SelectedSkillIndex.Subscribe(index =>
        {
            if (index < 0 || index >= skillList.Length)
            {
                return;
            }
            CurrentSkill = skillList[index];
        });

        SelectedSkillIndex.Value = 1;

        // ArcanaがNULLだったら愚者（逆）を入れる
        if (CurrentArcana == null)
            CurrentArcana = LoadManager.Instance.GetData(0, false);

        // 初期発動のアルカナを発動する
        CurrentArcana.ExecuteArcanaEffect(ASkillCategory.StartEffect, this);

        // 最大体力を設定
        CurrentHealth.Value = status.GetMaxHp();

        // エフェクトの初期化
        ActiveEffects.Clear();

        // ステータスを新品に入れ替える
        status = new PlayerStatus();

        PlayerUIManager playerUIManager = PlayerUIManager.Instance;

        // コンポーネントの初期化
        movement.Initialize(this);
        playerAttack.Initialized(this);
        playerAnimator.Initialize(PlayerIndex.Value);
        playerSkill.Initialize(this, playerUIManager, skillList);
        aimCursor.Initialize();
        playerUIManager.Initialize(this);
    }


    private void Update()
    {
        // 操作できない場合は移動を停止して処理を終了
        if (!CanControl) { movement.StopMovement(); return; }

        if(HaveEffect(EffectList.Stun, true)) { movement.StopMovement(); return; }

        // スキルのクールタイムを更新
        playerSkill.UpdateAllCoolTimes();

        // 移動処理の更新
        movement.UpdateMovement();

        // ジャンプ状態の更新


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
        aimCursor.AimUpdate();
    }

    // --- ActionHandlerから統合したメソッド群 ---

    public void ExecuteAttack()
    {
        // TODO : 後でPlayerRootに移植する

        //const int ATTACK_ACTION_INDEX = 0;
        //if (!IsActionReady(ATTACK_ACTION_INDEX)) return;

        //// CT開始
        //StartActionCoolTime(ATTACK_ACTION_INDEX, 0.5f); // ※本来の攻撃CTを取得して入れてください

        // エンペラーオーラ状態なら攻撃不可
        if (HaveEffect(EffectList.EnperorAura, true)) { return; }

        playerAttack.ResetList();
        playerAttack.NormalAttackActive();
        CurrentArcana?.ExecuteArcanaEffect(ASkillCategory.SkillEffect,this);
    }

    /// <summary> 移動実行メソッド </summary>
    public void ExecuteMove(float rawInput)
    {
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

        // 地面に接地している場合のみジャンプ可能
        if (rayInput.IsGrounded(movement.GetRigidbody()))
        {
            movement.JumpActive();
        }
    }

    /// <summary> スキル選択実行メソッド </summary>
    public void ExecuteSkillSelect(int direction)
    {
        int newSkillNo = playerSkill.SkillSelect(SelectedSkillIndex.Value, direction);

        // 選択スキル番号を更新
        SelectedSkillIndex.Value = newSkillNo;

        // エイム方法の切り替え
        if (newSkillNo != GameConfig.SKILL_HOPPER_MAX)
        {
            var aimSelect = CurrentSkill?.GetAimSelect();
            if (aimSelect != null) aimCursor.SelectAim((AimSelect)aimSelect);
        }
    }

    // / <summary> スキル使用実行メソッド </summary>
    public void ExecuteSkillUse()
    {
        // サイレンス状態ならスキル使用不可
        if (HaveEffect( EffectList.Silence, false)) { return; }

        // 選択中のスキルがクールタイム中であれば、処理を中断
        if (!playerSkill.IsActionReady(SelectedSkillIndex.Value + 1)) { return; }

        playerSkill.SkillUse(CurrentArcana,CurrentSkill,SelectedSkillIndex.Value);

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
    public AimCursor GetAimCursor() { return aimCursor; }


    // 全ての効果の数を取得するメソッド
    public int GetAllEffectCount() { return ActiveEffects.Count;}

    // すべての効果をクリアするメソッド
    public void ClearAllEffects() { ActiveEffects.Clear(); }

    public SpriteRenderer GetMagicStart() { return magicStart; }
    public PlayerStatus GetPlayerStatus() => status;
    public Arcana GetArcana() { return CurrentArcana; }
    public Skill GetCurrentSkill() { return CurrentSkill; }
    public Skill[] GetSkill() { return skillList; }


    /**
 * --------- セッター ---------
 */
    public void SetMoveSpeed(float speed) { status.SetSpeed(speed); }

    public void SetSkills(Skill[] skills)
    {
        // スキルリストをクリアして新しいスキルを追加
        for (int i = 0; i < skillList.Length; i++)
        {
            // 古いスキルを削除
            skillList[i] = null;

            // 新しいスキルを設定
            skillList[i] = skills.Length > i ? skills[i] : null;
        }
    }

    public void SetArcana(Arcana arcana)
    {
        CurrentArcana = arcana;
    }
}
