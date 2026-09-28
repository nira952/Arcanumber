using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// プレイヤーのスキル用コンポーネント
/// </summary>
public class NetworkPlayer : MonoBehaviour
{
    // --- 参照用 ---

    private PlayerRoot playerRoot;
    private AimCursor aimCursor;

    // --- クールタイム管理 ---

    private float[] currentCoolTimes = new float[GameConfig.COOLTIME_HOPPER_MAX];
    private float attackCoolTimeDuration = 0.5f; //通常攻撃のクールタイムの時間

    private Skill[] skillList = new Skill[GameConfig.SKILL_HOPPER_MAX];
    public ReactiveProperty<int> SelectedSkillIndex { get; } = new(0);

    // --- イベント ---
    public static event Action<NetworkPlayer, float> OnTakeDamageEvent;

    public void Initialize(PlayerRoot playerRoot, AimCursor aimCursor, Skill[] skillList)
    {
        this.playerRoot = playerRoot;
        this.aimCursor = aimCursor;
        this.skillList = skillList;

        //クールタイムリセット
        for (int i = 0; i < currentCoolTimes.Length; i++) { currentCoolTimes[i] = 0f; }

        ChangeColor();
    }


    /// <summary>
    /// スキル選択用メソッド
    /// </summary>
    /// <param name="direction"></param>
    public void SkillSelect(int direction)
    {
        // 変更方向から次のスキル番号を計算
        int newSkillNo = SelectedSkillIndex.Value + direction;

        // スキル番号が範囲外になった場合の処理
        if (newSkillNo < 0) newSkillNo = GameConfig.SKILL_HOPPER_MAX;
        else if (newSkillNo > GameConfig.SKILL_HOPPER_MAX) newSkillNo = 0;

        // 現在のアルカナスキルを取得
        Arcana currentArcana = playerRoot.GetArcana();

        // アルカナスキルスロットが選択されている場合、アルカナスキルがコマンドスキルでない場合は通常スキルに戻す
        bool isArcanaSlot = (newSkillNo == GameConfig.SKILL_HOPPER_MAX);
        bool isCommandArcana = currentArcana != null && currentArcana.GetASkillCategory() == ASkillCategory.Command;

        if (isArcanaSlot && !isCommandArcana)
            newSkillNo = (direction > 0) ? 0 : GameConfig.SKILL_HOPPER_MAX - 1;

        // スキル番号を更新
        SelectedSkillIndex.Value = newSkillNo;

        // エイム変更
        if (newSkillNo != GameConfig.SKILL_HOPPER_MAX)
        {
            var aimSelect = GetCurrentSkill()?.GetAimSelect();
            if (aimSelect != null) aimCursor.SelectAim((AimSelect)aimSelect);
        }

        // UI更新
        //PlayerUIManager.Instance.SkillFrameChange(this);
    }

    /// <summary>
    /// 実際にスキルを使用する
    /// </summary>
    public void SkillUse(Arcana arcana, Skill activeSkill)
    {
        // 現在選択されているスキル番号を取得
        int currentNo = SelectedSkillIndex.Value;

        // 選択中のスキルがクールタイム中であれば、処理を中断
        if (!IsActionReady(currentNo)) { return; }


        // 選択中のスキルがアルカナスキルの場合、アルカナスキルを発動
        if (currentNo == GameConfig.SKILL_HOPPER_MAX)
        {
            // 発動型のアルカナスキルを実行
            arcana?.ExecuteArcanaEffect(ASkillCategory.Command, this);

            // アルカナスキルのクールタイムを開始
            StartActionCoolTime(GameConfig.SKILL_ARCANA, arcana.GetCoolTime());
        }
        else // 選択中のスキルが通常スキルの場合、通常スキルを発動
        {
            if (activeSkill == null)
            {
                Debug.LogWarning($"Skill is null for skill number {currentNo}. Cannot execute skill.");
                return;
            }

            // SkillManagerへスキル発動を依頼
            SkillManager.Instance.RequestSkill(playerRoot, SelectedSkillIndex.Value);

            // 通常スキルのクールタイムを開始
            StartActionCoolTime(currentNo, activeSkill.GetCoolTime());

        }

        // スキル使用時に発動するアルカナを実行
        arcana?.ExecuteArcanaEffect(ASkillCategory.SkillEffect, this);
    }


    /**
     * --------- ゲッター ---------
     */
    public int GetNetworkId()
    {
        //playerRoot や NetworkObject が null の場合は safe に -1 を返す
        if (playerRoot == null)
            return -1;

        return playerRoot.PlayerIndex.Value; //または NetworkObject.OwnerClientId など
    }
    public float GetNowHP() { return playerRoot.CurrentHealth.Value; }
    public int GetNowJump() { return playerRoot.Nowjump.Value; }
    public Arcana GetArcana() { return playerRoot.GetArcana(); }
    public Skill[] GetSkill() { return skillList; }
    public int GetSkillNo() { return SelectedSkillIndex.Value; }
    public Skill GetNoSkill() { return skillList[SelectedSkillIndex.Value]; }
    public PlayerStatus GetPlayerStatus() { return playerRoot.GetPlayerStatus(); }
    public List<EffectAbility> GetHaveEffect() { return playerRoot.ActiveEffects.ToList(); }

    public PlayerRoot GetPlayerController() { return playerRoot; }

    public float GetSkillCoolTime(int index)
    {
        if (index >= 0 && index < currentCoolTimes.Length)
            return currentCoolTimes[index];
        return 0f; // 範囲外の場合は0を返す
    }
    public float GetAttackCoolTimeDuration() => attackCoolTimeDuration;


    /// <summary>
    /// 現在選択されているスキルを取得する
    /// </summary>
    /// <returns></returns>
    public Skill GetCurrentSkill() { return playerRoot.GetCurrentSkill(); }

    /// <summary>
    /// 選択されているスキル番号からクールタイムのインデックスを取得する
    /// </summary>
    /// <param name="skillNo"> スキル番号 </param>
    public float GetCoolTimeIndex(int skillNo)
    {
        // スキル番号が有効な範囲内であることを確認
        if (skillNo >= 0 && skillNo < skillList.Length)
        {
            // 現在選択されているスキルのクールタイムを返す
            return GetCurrentSkill().GetCoolTime();
        }
        return -1; // 無効なインデックスを返す
    }


    /**
     * --------- セッター ---------
     */

    public void SetHaveEffect(EffectAbility effect)
    {
        playerRoot.ActiveEffects.Add(effect);
    }

    /// <summary>
    /// ジャンプのリセット
    /// </summary>
    public void JumpReset() { playerRoot.Nowjump.Value = 0; }

    /// <summary>
    /// クールタイムの管理
    /// </summary>
    public bool IsActionReady(int index) => currentCoolTimes[index] <= 0f;

    /// <summary>
    /// クールタイムの開始
    /// </summary>
    public void StartActionCoolTime(int index, float duration)
    {
        if (index >= 0 && index < currentCoolTimes.Length)
        {
            currentCoolTimes[index] = CoolTimeValue(duration);
        }
    }


    private float CoolTimeValue(float duration)
    {
        // エフェクトの値を取得し、クールタイム短縮を計算
        float effectMultiplier = playerRoot.GetEffectValue(EffectList.CoolTimeReduction);
        float reductionRate = 1.0f - (effectMultiplier - 1.0f);

        // クールタイム短縮を適用し、下限を確保して返す
        return Mathf.Max(0.1f, duration * reductionRate);
    }

    /// <summary>
    /// クールタイムの更新
    /// </summary>
    public void UpdateAllCoolTimes()
    {
        for (int i = 0; i < currentCoolTimes.Length; i++)
        {
            if (currentCoolTimes[i] > 0f)
            {
                currentCoolTimes[i] -= Time.deltaTime;
                //マイナス防止
                if (currentCoolTimes[i] < 0f) currentCoolTimes[i] = 0f;
            }
        }
        PlayerUIManager.Instance.UpdateSkillCoolTimeUI(this);
    }


    /// <summary>
    /// 予備動作の色変更
    /// </summary>
    public void ChangeColor()
    {
        SpriteRenderer renderer = playerRoot.GetMagic();
        renderer.color = playerRoot.GetPlayerStatus().GetCharaColor(GetNetworkId());
    }

    /// <summary>
    /// ダメージ処理
    /// </summary>
    public void TakeDamage(float damage)
    {
        Arcana arcana = playerRoot.GetArcana();

        // TODO : 最終的にPlayerRootに吸収

        if (playerRoot.CurrentHealth.Value <= 0)
        {
            // 死亡時にアルカナスキルの発動
            arcana.ExecuteArcanaEffect(ASkillCategory.DeathEffect, this);
        }
        //ダメージアルカナスキルの発動
        if (arcana.GetASkillCategory() == ASkillCategory.DamageEffect)
        {
            OnTakeDamageEvent?.Invoke(this, damage);
        }
    }
}