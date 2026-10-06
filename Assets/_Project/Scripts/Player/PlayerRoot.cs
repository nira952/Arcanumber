using ObservableCollections;
using R3;
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
    private RuntimeAnimatorController currentAnimator;

    [Header("Components")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerRayInput rayInput;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private AimCursor aimCursor;
    [SerializeField] private PlayerSkiller playerSkill;

    // --- プレイヤー情報・入力状態 (省略せずにそのまま使用) ---
    public ReactiveProperty<int> PlayerIndex { get; } = new(-1);
    public ReactiveProperty<string> PlayerName { get; } = new("Player");
    public ReactiveProperty<float> CurrentHealth { get; } = new(100);
    public ReactiveProperty<bool> CanMove { get; } = new(true);
    public ReactiveProperty<bool> IsDown { get; } = new(false);
    public ObservableList<EffectAbility> ActiveEffects { get; } = new();
    public ReactiveProperty<int> SelectedSkillIndex { get; } = new(0);


    private Vector3 currentDirection = Vector3.right;
    /// <summary> 入力を受け付けて制御できるかどうかを示します </summary>
    public bool CanControl => 
        !IsDown.Value && (GameManager.Instance == null || GameManager.Instance.StateRx.CurrentValue == GameState.Playing);

    public void OwnerInitialize()
    {
        // プレイヤー名をPlayerDataManagerから取得
        PlayerName.Value = PlayerDataManager.Instance.LocalPlayerName;

        //　ArcanaとスキルをPlayerDataManagerから取得
        int myIndex = PlayerIndex.Value;

        // 自分のスキルとアルカナを取得
        skillList = PlayerDataManager.Instance.GetMySkills();
        CurrentArcana = PlayerDataManager.Instance.GetMyArcana();


        // 選択スキル番号が変更されたときにCurrentSkillを更新する
        SelectedSkillIndex.Subscribe(index =>
        {
            if (index < 0 || index >= skillList.Length){ return; }
            CurrentSkill = skillList[index];
        });

        // ArcanaがNULLだったら愚者（逆）を入れる
        if (CurrentArcana == null)
            CurrentArcana = LoadManager.Instance.GetData(0, false);

        // エフェクトの初期化
        ActiveEffects.Clear();

        // 初期発動のアルカナを発動する
        CurrentArcana.ExecuteArcanaEffect(ASkillCategory.StartEffect, this);

        // 最大体力を設定
        CurrentHealth.Value = status.GetMaxHp();

        // ステータスを新品に入れ替える
        status = new PlayerStatus();




        PlayerUIManager playerUIManager = PlayerUIManager.Instance;

        // コンポーネントの初期化
        movement.Initialize(this);
        rayInput.Initialize();
        playerAttack.Initialized(this, false);
        currentAnimator = playerAnimator.Initialize(PlayerIndex.Value);
        playerSkill.Initialize(this, playerUIManager, skillList);
        aimCursor.Initialize(PlayerIndex.Value);
        playerUIManager.Initialize(this);

        SelectedSkillIndex.Value = 0;

        ExecuteSkillSelect(0); // 初期スキル選択を行う

    }


    private void Update()
    {
        // 操作できない場合は移動を停止して処理を終了
        if (!CanControl) { movement.StopMovement(); return; }

        // 状態異常の更新処理
        for (int i = ActiveEffects.Count - 1; i >= 0; i--)
        {
            var effect = ActiveEffects[i];

            // 1. nullチェック（インスタンス自体が null の場合は削除して次へ）
            if (effect == null)
            {
                Debug.LogWarning($"[PlayerRoot] ActiveEffects[{i}] が null のため削除しました。");
                ActiveEffects.RemoveAt(i);
                continue;
            }

            // 2. 内部データ（ScriptableObject等）の健全性チェック
            // ※GetEffect() などで保持データが null でないか確認（1つ目のエラー対策）
            if (effect.GetEffect() == null)
            {
                Debug.LogWarning($"[PlayerRoot] {effect} の内部データ (Effect) が null のため削除しました。");
                ActiveEffects.RemoveAt(i);
                continue;
            }

            // 3. 継続時間を減らす
            effect.DecreaseTime(Time.deltaTime);

            // 4. 時間切れなら Tick を実行せずに削除
            if (effect.IsExpired)
            {
                ActiveEffects.RemoveAt(i);
                continue;
            }

            // 5. 毒・回復等の Tick 処理を実行（safe に実行可能）
            effect.ExecuteTick(Time.deltaTime, this);
        }

        if (HaveEffect(EffectList.Stun, false)) { movement.StopMovement(); return; }

        // スキルのクールタイムを更新
        playerSkill.UpdateAllCoolTimes();



        if (!CanMove.Value) { movement.StopMovement(); return; }
        
        // 移動処理の更新
        movement.UpdateMovement();

        

    }

    public void LateUpdate()
    {
        if (!CanControl) return;
        aimCursor.AimUpdate();
    }

    // --- ActionHandlerから統合したメソッド群 ---

    public void ExecuteAttack()
    {
        return;

        if (!CanMove.Value) { return; }

        // エンペラーオーラ状態なら攻撃不可
        if (HaveEffect(EffectList.EnperorAura, true)) { return; }

        playerAttack.ResetList();
        playerAttack.NormalAttackActive(currentDirection);
        CurrentArcana?.ExecuteArcanaEffect(ASkillCategory.SkillEffect,this);

        // アニメーション再生
        playerAnimator.PlayMagicAnimation();

    }

    /// <summary> 移動実行メソッド </summary>
    public void ExecuteMove(float rawInput)
    {
        if (!CanMove.Value) { return; }
        
        // 移動方向の反転処理
        bool isChangeMove = HaveEffect(EffectList.Reverse, false);

        float finalInput = !isChangeMove ? rawInput : -rawInput;

        movement.SetMoveDirection(finalInput);

        // 向きの反転
        currentDirection = playerAnimator.Flip(rawInput);

        // ダッシュアニメーションの切り替え
        bool isDashing = Mathf.Abs(rawInput) > 0.01f;
        playerAnimator.SetDash(isDashing);

    }

    /// <summary> ジャンプ実行メソッド </summary>
    public void ExecuteJump()
    {
        if (!CanMove.Value) { return; }

        // ジャンプ禁止状態ならジャンプ不可
        if (HaveEffect(EffectList.Stun, false)) { return; }
        if (HaveEffect(EffectList.NoJump, false)) { return; }

        bool isGrounded = rayInput.IsGrounded(movement.GetRigidbody());

        Debug.Log(isGrounded);

        // 地面に接地している場合のみジャンプ可能
        if (isGrounded)
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
        if (!CanMove.Value) { return; }

        // サイレンス状態ならスキル使用不可
        if (HaveEffect( EffectList.Silence, false)) { return; }

        // 選択中のスキルがクールタイム中であれば、処理を中断
        if (!playerSkill.IsActionReady(SelectedSkillIndex.Value)) { return; }

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

    /// <summary>
    /// 自分の頭上に Ground（天井）があるかどうかを判定するメソッド (2D用)
    /// </summary>
    public bool HasCeiling()
    {
        // プレイヤーの位置（少し足元なら調整してください）から真上にレイを飛ばす距離
        float rayDistance = 20f;

        // Ground レイヤーのマスクを取得（レイヤー名が "Ground" の場合）
        int groundLayerMask = LayerMask.GetMask("Ground");

        // 真上に向かってレイキャストを飛ばす
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.up, rayDistance, groundLayerMask);

        // ヒットしたものが存在すれば、頭上に天井がある
        return hit.collider != null;
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
    public Skill[] GetSkill() { return skillList; }

    public RuntimeAnimatorController GetCurrentAnimator() { return currentAnimator; }


    /**
 * --------- セッター ---------
 */
    public void SetPlayerIndex(int index)
    {
        if(index >= 0)
        {
            PlayerIndex.Value = index;
        }
    }
    public void SetMoveSpeed(float speed) { status.SetSpeed(speed); }

    public void SetSkill(Skill[] skills)
    {
        skillList = skills;
    }

    public void SetArcana(Arcana arcana)
    {
        CurrentArcana = arcana;
    }
}
