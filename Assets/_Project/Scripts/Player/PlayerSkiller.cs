using UnityEngine;

/// <summary>
/// プレイヤーのスキル用コンポーネント
/// </summary>
public class PlayerSkiller : MonoBehaviour
{
    // --- 参照用 ---
    private PlayerRoot playerRoot;

    private PlayerUIManager playerUIManager;

    // --- クールタイム管理 ---

    [SerializeField] private float[] currentCoolTimes = new float[GameConfig.COOLTIME_HOPPER_MAX];
    private float attackCoolTimeDuration = 0.5f; //通常攻撃のクールタイムの時間

    [SerializeField] private float[] maxCoolTimes = new float[GameConfig.COOLTIME_HOPPER_MAX];

    // --- イベント ---

    public void Initialize(PlayerRoot playerRoot, PlayerUIManager playerUIManager, Skill[] skillList)
    {
        this.playerRoot = playerRoot;
        this.playerUIManager = playerUIManager;

        foreach (var skill in skillList)
        {
            // スキルが存在するかデバッグログを流す
            if (skill != null)
            {
                Debug.Log($"Skill {skill.GetSkillName()} is loaded.");
            }
            else
            {
                Debug.LogWarning("A skill slot is empty.");
            }
        }

        //クールタイムリセット
        for (int i = 0; i < currentCoolTimes.Length; i++) { currentCoolTimes[i] = 0f; }

        //最大クールタイムを初期化
        for (int i = 0; i < maxCoolTimes.Length; i++)
        {
            Debug.Log(i);

            // アルカナスキルの場合はArcanaからクールタイムを取得、それ以外は通常スキルから取得
            if (i == GameConfig.SKILL_ARCANA)
            {
                maxCoolTimes[i] = playerRoot.GetArcana()?.GetCoolTime() ?? 0f;

            }
            else
            {
                maxCoolTimes[i] = skillList[i]?.GetCoolTime() ?? 0f;

            }
        }


        ChangeColor();
    }


    /// <summary>
    /// スキル選択用メソッド
    /// </summary>
    public int SkillSelect(int nowSkillIndex, int direction)
    {
        // 変更方向から次のスキル番号を計算
        int newSkillNo = nowSkillIndex + direction;

        // スキル番号が範囲外になった場合の処理
        if (newSkillNo < 0) newSkillNo = GameConfig.SKILL_HOPPER_MAX;
        else if (newSkillNo > GameConfig.SKILL_HOPPER_MAX) newSkillNo = 0;

        // 現在のアルカナスキルを取得
        Arcana currentArcana = playerRoot.GetArcana();

        // アルカナスキルスロットが選択されている場合、アルカナスキルが選択可能スキルでない場合は通常スキルに戻す
        bool isArcanaSlot = (newSkillNo == GameConfig.SKILL_HOPPER_MAX);
        bool isCommandArcana = currentArcana != null && currentArcana.GetASkillCategory() == ASkillCategory.Command;

        // アルカナスキルスロットが選択されているが、コマンドアルカナスキルがない場合は、通常スキルに戻す
        if (isArcanaSlot && !isCommandArcana)
            newSkillNo = (direction > 0) ? 0 : GameConfig.SKILL_HOPPER_MAX - 1;

        // UI更新
        return newSkillNo;
    }

    /// <summary>
    /// 実際にスキルを使用する
    /// </summary>
    public void SkillUse(Arcana arcana, Skill activeSkill,int index)
    {
        // 選択中のスキルがアルカナスキルの場合、アルカナスキルを発動
        if (index == GameConfig.SKILL_HOPPER_MAX)
        {
            // 発動型のアルカナスキルを実行
            arcana?.ExecuteArcanaEffect(ASkillCategory.Command, playerRoot);

            // アルカナスキルのクールタイムを開始
            StartActionCoolTime(GameConfig.SKILL_ARCANA, arcana.GetCoolTime());
        }
        else // 選択中のスキルが通常スキルの場合、通常スキルを発動
        {
            if (activeSkill == null)
            {
                Debug.LogWarning($"Skill is null for skill number {index}. Cannot execute skill.");
                return;
            }

            // SkillManagerへスキル発動を依頼
            SkillManager.Instance.RequestSkill(playerRoot);

            // 通常スキルのクールタイムを開始
            StartActionCoolTime(index, activeSkill.GetCoolTime());

        }

        // スキル使用時に発動するアルカナを実行
        arcana?.ExecuteArcanaEffect(ASkillCategory.SkillEffect, playerRoot);
    }


    /**
     * --------- セッター ---------
     */


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

        if (playerUIManager != null)
        {
            playerUIManager.UpdateSkillCoolTimeUI(currentCoolTimes, maxCoolTimes);
        }
    }

    /// <summary>
    /// 予備動作の色変更
    /// </summary>
    public void ChangeColor()
    {
        SpriteRenderer renderer = playerRoot.GetMagicStart();
        renderer.color = playerRoot.GetPlayerStatus().GetCharaColor(playerRoot.PlayerIndex.Value);
    }

}