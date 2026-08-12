using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : SingletonMonoBehaviour<PlayerUIManager>
{

    [Header("スキル関係")]
    [SerializeField] private GameObject[] skillBlocks = new GameObject[6];
    [SerializeField] private Image[] skillIcons = new Image[6];
    [SerializeField] private Image[] skillTimeIcons = new Image[6];
    [SerializeField] private TextMeshProUGUI[] skillTimeTexts = new TextMeshProUGUI[6];
    [SerializeField] private GameObject[] skillFrame = new GameObject[5];


    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(NetworkPlayer[] player, int playerId)
    {
        SkillUIInitialize(player[playerId]);
        FrameColorChange(player[playerId]);
    }
    public void Initialize(PlayerDataManager player)
    {
        SkillUIInitialize(player);
        FrameColorChange(player);
    }


    /// <summary>
    /// スキルUIの初期化
    /// </summary>
    void SkillUIInitialize(NetworkPlayer player)
    {
        //クールタイムを元に戻す
        for (int i = 0; i < skillBlocks.Length; i++)
        {
            if (i < player.GetSkill().Count())
            {
                skillTimeIcons[i].fillAmount = 1f;
                skillTimeTexts[i].text = "";
            }
        }

        //スキルのスプライトを変更
        Skill[] pSkills = player.GetSkill();
        for (int i = 0; i < pSkills.Length; i++)
        {
            skillIcons[i + 1].sprite = pSkills[i].GetSprite();
            skillTimeIcons[i + 1].sprite = pSkills[i].GetSprite();
        }
        SkillFrameChange(player);
    }
    void SkillUIInitialize(PlayerDataManager playerDataManager)
    {
        if (playerDataManager == null) return;

        Skill[] mySkills = playerDataManager.GetMySkills();
        if (mySkills == null) return;

        // クールタイムを元に戻す
        for (int i = 0; i < skillBlocks.Length; i++)
        {
            if (i < mySkills.Length)
            {
                if (i < skillTimeIcons.Length && skillTimeIcons[i] != null)
                    skillTimeIcons[i].fillAmount = 1f;
                if (i < skillTimeTexts.Length && skillTimeTexts[i] != null)
                    skillTimeTexts[i].text = "";
            }
        }

        // スキルのスプライトを変更
        for (int i = 0; i < mySkills.Length; i++)
        {
            if (mySkills[i] != null)
            {
                int uiIndex = i + 1; // 0番通常攻撃などのオフセットに合わせて調整
                if (uiIndex < skillIcons.Length && skillIcons[uiIndex] != null)
                {
                    skillIcons[uiIndex].sprite = mySkills[i].GetSprite();
                }
                if (uiIndex < skillTimeIcons.Length && skillTimeIcons[uiIndex] != null)
                {
                    skillTimeIcons[uiIndex].sprite = mySkills[i].GetSprite();
                }
            }
        }
    }

    /// <summary>
    /// スキルとアルカナのクールタイム状態をUIにリアルタイム反映する
    /// </summary>
    public void UpdateSkillCoolTimeUI(NetworkPlayer player)
    {
        //通常攻撃のクールタイム更新
        float currentAttack = player.GetSkillCoolTime(0);
        float maxAttack = player.GetAttackCoolTimeDuration();
        UpdateBlockUI(0, currentAttack, maxAttack);

        //通常スキル
        Skill[] skills = player.GetSkill();
        for (int i = 0; i < skills.Length; i++)
        {
            //１～４までのUI更新
            int uiIndex = PlayerUtility.GetCoolTimeIndex(i);

            if (uiIndex < skillBlocks.Length && skills[i] != null)
            {
                float currentCoolTime = player.GetSkillCoolTime(uiIndex);
                float maxCoolTime = PlayerUtility.CoolTimeValue(player, skills[i].GetCoolTime());

                //UIの配列番号をそのまま渡す
                UpdateBlockUI(uiIndex, currentCoolTime, maxCoolTime);
            }
        }

        //アルカナ枠のUI更新
        int arcanaUIIndex = 5;
        Arcana playerArcana = player.GetArcana();
        if (playerArcana != null && arcanaUIIndex < skillBlocks.Length)
        {
            //クールタイムを更新
            float currentCoolTime = player.GetSkillCoolTime(5);
            float maxCoolTime = playerArcana.GetCoolTime();

            UpdateBlockUI(arcanaUIIndex, currentCoolTime, maxCoolTime);
        }
    }

    /// <summary>
    /// 指定されたスロットの補助メソッド
    /// </summary>
    private void UpdateBlockUI(int uiIndex, float current, float max)
    {
        if (current > 0f && max > 0f)
        {
            float elapsed = max - current; //経過した時間
            skillTimeIcons[uiIndex].fillAmount = elapsed / max; //0から1に向かって増えていく

            //残り秒数をテキストに表示（9秒以下になると小数点が出てくる）
            skillTimeTexts[uiIndex].text = current >= 10f
                            ? $"{Mathf.CeilToInt(current):F0}"
                            : $"{current:F1}";
        }
        else
        {
            //満タンの状態
            skillTimeIcons[uiIndex].fillAmount = 1f;
            skillTimeTexts[uiIndex].text = "";
        }
    }


    /// <summary>
    /// フレーム変更
    /// </summary>
    public void SkillFrameChange(NetworkPlayer player)
    {
        int index = player.GetSkillNo();
        if (index >= 0 && index < skillFrame.Length)
        {
            //すべての枠消す
            foreach (var frame in skillFrame)
                frame.SetActive(false);

            //指定されたインデックスだけ
            skillFrame[index].SetActive(true);
        }
    }

    /// <summary>
    /// 枠の色変更
    /// </summary>
    public void FrameColorChange(NetworkPlayer player)
    {
        int pIndex = player.GetNetworkId();
        Color color;
        switch (pIndex)
        {
            case 0:
                color = Color.red;
                break;
            case 1:
                color = Color.blue;
                break;
            case 2:
                color = Color.green;
                break;
            case 3:
                color = Color.yellow;
                break;
            default:
                color = Color.white;
                break;
        }
        foreach (var frame in skillFrame)
            frame.GetComponent<Image>().color = color;
    }
    public void FrameColorChange(PlayerDataManager playerDataManager)
    {
        if (playerDataManager == null) return;

        // PlayerDataManager から自分のロビーインデックス（またはID）を取得する
        int pIndex = playerDataManager.GetMyLobbyIndex();
        Color color;
        switch (pIndex)
        {
            case 0:
                color = Color.blue;
                break;
            case 1:
                color = Color.red;
                break;
            case 2:
                color = Color.green;
                break;
            case 3:
                color = Color.yellow;
                break;
            default:
                color = Color.white;
                break;
        }

        foreach (var frame in skillFrame)
        {
            if (frame != null)
            {
                var img = frame.GetComponent<Image>();
                if (img != null)
                    img.color = color;
            }
        }
    }
}

