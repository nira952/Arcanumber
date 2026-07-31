using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ネットワーク用のプレイヤー
/// </summary>
public class NetworkPlayer : MonoBehaviour
{
    private PlayerRoot playerRoot;

    private float[] currentCoolTimes = new float[6];
    private float attackCoolTimeDuration = 0.5f; //通常攻撃のクールタイムの時間

    public static event System.Action<NetworkPlayer, float> OnTakeDamageEvent;


    private bool isSpawn = false;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(PlayerRoot playerRoot, bool isLocalPlayer)
    {
        this.playerRoot = playerRoot;

        //リセット処理
        ResetToInitialState();

        isSpawn = true;
    }


    /**
     * --------- ゲッター ---------
     */
    public int GetNetworkId()
    {
        // playerRoot や NetworkObject が null の場合は safe に -1 を返す
        if (playerRoot == null)
        {
            return -1;
        }

        return playerRoot.PlayerIndex.Value; // または NetworkObject.OwnerClientId など
    }
    public string GetPlayerName() { return playerRoot.name; }
    public float GetNowHP() {  return playerRoot.CurrentHealth.Value; }
    public int GetNowJump() {  return playerRoot.Nowjump.Value; }
    public Arcana GetArcana() { return playerRoot.CurrentArcana; }
    public Skill[] GetSkill() { return playerRoot.SkillList; }
    public int GetSkillNo() {  return playerRoot.SelectedSkillIndex.Value; }
    public Skill GetNoSkill() { return playerRoot.SkillList[playerRoot.SelectedSkillIndex.Value]; }
    public PlayerStatus GetPlayerStatus() {  return playerRoot.GetPlayerStatus(); }
    public List<EffectAbility> GetHaveEffect() { return playerRoot.ActiveEffects.Value; }
    public GameObject GetMagicStart() { return gameObject; }

    public PlayerRoot GetPlayerController() { return playerRoot; }

    public float GetSkillCoolTime(int index) 
    {
        if (index >= 0 && index < currentCoolTimes.Length)
            return currentCoolTimes[index];
        return 0f; // 範囲外の場合は0を返す
    }
    public float GetAttackCoolTimeDuration() => attackCoolTimeDuration;

    /**
     * --------- セッター ---------
     */
    public void SetNowHp(float nowHp) { playerRoot.CurrentHealth.Value = (int)nowHp; }
    public void SetHaveEffect(EffectAbility effect) 
    { 
        playerRoot.ActiveEffects.Value.Add(effect);
    }
    public void SetSkillNo(int skillNo) { playerRoot.SelectedSkillIndex.Value = skillNo; }
    public void SetArcana(Arcana arcana) { playerRoot.CurrentArcana = arcana; }
    public void SetSkill(Skill skill, int sNum) { playerRoot.SkillList[sNum] = skill; }


    public void ResetToInitialState()
    {
        playerRoot.ResetToInitialState();

        //クールタイムリセット
        for (int i = 0; i < currentCoolTimes.Length; i++)
            currentCoolTimes[i] = 0f;

        //一番最初に発動するアルカナスキル
        playerRoot.CurrentArcana.ExecuteArcanaEffect(ASkillCategory.StartEffect, this);
    }

    /// <summary>
    /// ジャンプのリセット
    /// </summary>
    public void JumpReset() { playerRoot.Nowjump.Value = 0; }

    /// <summary>
    /// ジャンプのアクション
    /// </summary>
    private void RequestJump()
    {
        int maxJump = playerRoot.GetPlayerStatus() != null ? playerRoot.GetPlayerStatus().GetMaxJump() : 1;

        if (playerRoot.Nowjump.Value < maxJump)
        {
            playerRoot.Nowjump.Value++;
            playerRoot.GetActionController().ExecuteJumpLocal();
        }
    }

    /// <summary>
    /// 攻撃のアクション
    /// </summary>
    public void UseAttack() { ActionHandler.ExecuteAttack(this); }

    /// <summary>
    /// スキル変更のアクション
    /// </summary>
    public void ChangeSelectedSkill(int direction) { ActionHandler.ExecuteSkillChange(this, direction); }

    /// <summary>
    /// スキル選択のアクション
    /// </summary>
    public void UseCurrentSkill() { ActionHandler.ExecuteSkill(this); }

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
            currentCoolTimes[index] = PlayerUtility.CoolTimeValue(this, duration);
        }
    }

    public void SkillUpdate()
    {
        if (!isSpawn) { return; }

        UpdateAllCoolTimes(Time.deltaTime);
        PlayerUIManager.Instance.UpdateSkillCoolTimeUI(this);
    }
    /// <summary>
    /// クールタイムの更新
    /// </summary>
    public void UpdateAllCoolTimes(float deltaTime)
    {
        for (int i = 0; i < currentCoolTimes.Length; i++)
        {
            if (currentCoolTimes[i] > 0f)
            {
                currentCoolTimes[i] -= deltaTime;
                //マイナス防止
                if (currentCoolTimes[i] < 0f) currentCoolTimes[i] = 0f;
            }
        }
    }

    /// <summary>
    /// 予備動作の色変更
    /// </summary>
    //public void ChangeColor()
    //{
    //    SpriteRenderer renderer = magicStart.GetComponent<SpriteRenderer>();
    //    renderer.color = playerRoot.GetPlayerStatus().GetCharaColor(networkId);
    //}

    /// <summary>
    /// ダメージ処理
    /// </summary>
    public void TakeDamage(float damage)
    {
        playerRoot.ApplyDamage((int)damage);

        if (playerRoot.CurrentHealth.Value <= 0)
            playerRoot.CurrentArcana.ExecuteArcanaEffect(ASkillCategory.DeathEffect, this);
        //ダメージアルカナスキルの発動
        if (playerRoot.CurrentArcana.GetASkillCategory() == ASkillCategory.DamageEffect)
            OnTakeDamageEvent?.Invoke(this, damage);
    }

    /// <summary>
    /// 回復処理
    /// </summary>
    public void Heal(float healAmount)
    {
        playerRoot.ApplyHeal((int)healAmount);
    }

    /// <summary>
    /// 期限切れのエフェクトを本物のリストから削除する
    /// </summary>
    public void CleanExpiredEffects()
    {
        //期限切れのエフェクトを取得
        var expiredEffects = playerRoot.ActiveEffects.Value.FindAll(e => e.IsExpired);

        //最後にリストから削除
        playerRoot.ActiveEffects.Value.RemoveAll(e => e.IsExpired);
    }
}
