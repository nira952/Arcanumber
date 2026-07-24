using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ネットワーク用のプレイヤー
/// </summary>
public class NetworkPlayer : MonoBehaviour
{
    [SerializeField] int networkId;  //ネットワークのID
    [SerializeField] string playerName;  //プレイヤーの名前
    [SerializeField] PlayerStatus status = new PlayerStatus();    //プレイヤーステータス
    [SerializeField] float nowHp;  //現在のHP
    [SerializeField] int nowJump = 0;   //現在のジャンプの回数
    Arcana arcana;  //持っているアルカナスキル
    [SerializeField] Skill[] skillList = new Skill[4];  //持っているスキルリスト
    [SerializeField] private float[] currentCoolTimes = new float[6];   //クールタイムの管理用変数
    private float attackCoolTimeDuration = 0.5f; //通常攻撃のクールタイムの時間
    [SerializeField] int skillNo = 0;    //現在合わせているスキルNo

    [SerializeField] List<EffectAbility> haveEffect = new List<EffectAbility>();  //持っているエフェクト

    [SerializeField] PlayerController pController;    //持っているプレイヤーコントローラー
    [SerializeField] private GameObject magicStart; //予備動作用のオブジェクト

    //ダメージを受けたときのイベント（NetworkPlayer, ダメージ量）
    public static event System.Action<NetworkPlayer, float> OnTakeDamageEvent;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(bool isLocalPlayer)
    {
        //リセット処理
        ResetToInitialState();

        //(アルカナ発動を分離)
        if (isLocalPlayer)
            InitializeLocalSettings();
    }

    /// <summary>
    /// ローカル用の初期化
    /// </summary>
    private void InitializeLocalSettings()
    {
        ChangeColor();
        //入力やコントローラー、アルカナ発動は「自分だけ」のものにする
        pController.SetIsMove(true);
        pController.SetIsJump(true);
        pController.Initialize();
        pController.OnAttackEvent += UseAttack;
        pController.OnSkillSelectEvent += ChangeSelectedSkill;
        pController.OnSkillUseEvent += UseCurrentSkill;
        pController.OnJumpEvent += RequestJump;
        //エイム設定
        GetPlayerController().GetAimCursor()
            .SelectAim(GetNoSkill().GetAimSelect());
    }

    private void OnDestroy()
    {
        if (pController != null)
        {
            pController.OnAttackEvent -= UseAttack;
            pController.OnSkillSelectEvent -= ChangeSelectedSkill;
            pController.OnSkillUseEvent -= UseCurrentSkill;
            pController.OnJumpEvent -= RequestJump;
        }
    }

    /**
     * --------- ゲッター ---------
     */
    public int GetNetworkId() { return networkId; }
    public string GetPlayerName() { return playerName; }
    public float GetNowHP() {  return nowHp; }
    public int GetNowJump() {  return nowJump; }
    public Arcana GetArcana() { return arcana; }
    public Skill[] GetSkill() { return skillList; }
    public int GetSkillNo() {  return skillNo; }
    public Skill GetNoSkill() { return skillList[skillNo]; }
    public PlayerStatus GetPlayerStatus() {  return status; }
    public List<EffectAbility> GetHaveEffect() { return haveEffect; }
    public PlayerController GetPlayerController() {  return pController; }
    public GameObject GetMagicStart() { return magicStart; }
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
    public void SetNowHp(float nowHp) { this.nowHp = nowHp; }
    public void SetHaveEffect(EffectAbility effect) 
    { 
        haveEffect.Add(effect); 
        BattleUIManager.Instance.StatusAddUpdate(this, effect);
    }
    public void SetHaveEffects(List<EffectAbility> effect) { this.haveEffect =  effect; }
    public void SetSkillNo(int skillNo) { this.skillNo = skillNo; }
    public void SetArcana(Arcana arcana) { this.arcana = arcana; }
    public void SetSkill(Skill skill, int sNum) { this.skillList[sNum] = skill; }

    /// <summary>
    /// リセット用のメソッド
    /// </summary>
    public void ResetToInitialState()
    {
        // ステータスを新品に入れ替える
        status = new PlayerStatus();
        //NULLだったら愚者（逆）を入れる
        if (arcana == null)
            arcana = LoadManager.Instance.GetData(0, false);
            //クールタイムリセット
        for (int i = 0; i < currentCoolTimes.Length; i++)
        currentCoolTimes[i] = 0f;
        //エフェクトリセット
        haveEffect.Clear();
        //一番最初に発動するアルカナスキル
        arcana.ExecuteArcanaEffect(ASkillCategory.StartEffect, this);
        SetNowHp(status.GetMaxHp());
    }

    /// <summary>
    /// ジャンプのリセット
    /// </summary>
    public void JumpReset() { nowJump = 0; }

    /// <summary>
    /// ジャンプのアクション
    /// </summary>
    private void RequestJump()
    {
        int maxJump = status != null ? status.GetMaxJump() : 1;

        if (nowJump < maxJump)
        {
            nowJump++;
            pController.ExecuteJump();
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
    public void ChangeColor()
    {
        SpriteRenderer renderer = magicStart.GetComponent<SpriteRenderer>();
        renderer.color = status.GetCharaColor(networkId);
    }

    /// <summary>
    /// ダメージ処理
    /// </summary>
    public void TakeDamage(float damage)
    {
        nowHp -= damage;
        if (nowHp < 0) nowHp = 0;
        SetNowHp(nowHp);
        if (nowHp <= 0)
            arcana.ExecuteArcanaEffect(ASkillCategory.DeathEffect, this);
        //ダメージアルカナスキルの発動
        if (arcana.GetASkillCategory() == ASkillCategory.DamageEffect)
            OnTakeDamageEvent?.Invoke(this, damage);
        BattleUIManager.Instance.hpSliderChange(this, this.GetNetworkId());
    }

    /// <summary>
    /// 回復処理
    /// </summary>
    public void Heal(float healAmount)
    {
        nowHp += healAmount;
        if (nowHp > status.GetMaxHp()) nowHp = status.GetMaxHp();
        SetNowHp(nowHp);
        BattleUIManager.Instance.hpSliderChange(this, this.GetNetworkId());
    }

    /// <summary>
    /// 期限切れのエフェクトを本物のリストから削除する
    /// </summary>
    public void CleanExpiredEffects()
    {
        //期限切れのエフェクトを取得
        var expiredEffects = haveEffect.FindAll(e => e.IsExpired);
        foreach (var e in expiredEffects)
        {
            //終了したエフェクトをUIから消す
            BattleUIManager.Instance.RemoveStatusUI(this, e);
        }
        //最後にリストから削除
        haveEffect.RemoveAll(e => e.IsExpired);
    }
}
