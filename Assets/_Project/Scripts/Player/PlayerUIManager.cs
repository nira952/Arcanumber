using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : SingletonMonoBehaviour<PlayerUIManager>
{
    [Header("通常攻撃関係")]

    [SerializeField] private GameObject normalAttackBlock;
    [SerializeField] private Image normalAttackIcon;
    [SerializeField] private Image normalAttackTimeIcon;
    [SerializeField] private TextMeshProUGUI normalAttackText;


    [Header("スキル関係")]
    [SerializeField] private GameObject[] skillBlocks = new GameObject[GameConfig.COOLTIME_HOPPER_MAX];
    [SerializeField] private Image[] skillIcons = new Image[GameConfig.COOLTIME_HOPPER_MAX];
    [SerializeField] private Image[] skillTimeIcons = new Image[GameConfig.COOLTIME_HOPPER_MAX];
    [SerializeField] private TextMeshProUGUI[] skillTimeTexts = new TextMeshProUGUI[GameConfig.COOLTIME_HOPPER_MAX];
    [SerializeField] private GameObject[] skillFrame = new GameObject[GameConfig.COOLTIME_HOPPER_MAX];

    // 各キャラクター用の色を設定する変数 (1:青, 2:赤, 3:緑, 4:黄)
    private Color[] playerColors = new Color[4] { Color.blue, Color.red, Color.green, Color.yellow };

    public void Initialize(PlayerRoot player)
    {
        Skill[] skills = player.GetSkill();
        Arcana arcana = player.GetArcana();
        int plIndex = player.PlayerIndex.Value;

        Debug.Log($"[PlayerUIManager] Player index: {plIndex}");
        SkillUIInitialize(skills, arcana);
        FrameColorChange(plIndex);

        // スキル選択のUI反映
        player.SelectedSkillIndex.Subscribe(index => SkillFrameChange(index)).AddTo(this);
    }

    /// <summary>
    /// スキルUIの初期化
    /// </summary>
    void SkillUIInitialize(Skill[] skills,Arcana arcana)
    {
        //クールタイムを元に戻す
        for (int i = 0; i < skills.Length; i++)
        {
            skillTimeIcons[i].fillAmount = 1f;
            skillTimeTexts[i].text = "";  
        }

        // スキルのスプライトを変更
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i] == null || skills[i].GetSprite() == null)
            {
                Debug.LogError($"[PlayerUIManager] Skill at index {i} is null or has no sprite.");
                continue;
            }

            Debug.Log($"[PlayerUIManager] Skill sprite: {skills[i].GetSprite().name}");

            skillIcons[i].sprite = skills[i].GetSprite();
            skillTimeIcons[i].sprite = skills[i].GetSprite();
        }

        // アルカナのスプライトを変更
        if(arcana.GetASkillCategory() != ASkillCategory.Command)
            skillTimeTexts[GameConfig.COOLTIME_HOPPER_MAX - 1].text = ""; // アルカナのテキストは空にする

        // スキル選択のUI反映
        SkillFrameChange(1);
    }
    /// <summary>
    /// スキルとアルカナのクールタイム状態をUIにリアルタイム反映する
    /// </summary>
    public void UpdateSkillCoolTimeUI(float[] currentSkillCT, float[] maxSkillCT)
    {
        for (int i = 0; i < currentSkillCT.Length; i++)
        {
            UpdateBlockUI(i, currentSkillCT[i], maxSkillCT[i]);
        }
    }



    /// <summary>
    /// 指定されたスロットの補助メソッド
    /// </summary>
    private void UpdateBlockUI(int index, float current, float max)
    {
        if (current > 0f)
        {
            float elapsed = max - current; //経過した時間
            skillTimeIcons[index].fillAmount = elapsed / max; //0から1に向かって増えていく

            string formattedTime = current >= 10f
                ? $"{Mathf.CeilToInt(current):F0}"
                : $"{current:F1}";

            //残り秒数をテキストに表示（9秒以下になると小数点が出てくる）
            skillTimeTexts[index].text = formattedTime;

            // 0.1より小さくなったら0にする
            if (current <= 0.1f)
            {
                skillTimeIcons[index].fillAmount = 1f;
                skillTimeTexts[index].text = "";
            }

        }

    }

    public void UpdateAttackCoolTimeUI(float current, float max)
    {
        UpdateNormalAttackUI(current, max);
    }
    private void UpdateNormalAttackUI(float current, float max)
    {
        if (current > 0f)
        {
            float elapsed = max - current; //経過した時間
            normalAttackTimeIcon.fillAmount = elapsed / max; //0から1に向かって増えていく
            string formattedTime = current >= 10f
                ? $"{Mathf.CeilToInt(current):F0}"
                : $"{current:F1}";
            //残り秒数をテキストに表示（9秒以下になると小数点が出てくる）
            normalAttackText.text = formattedTime;
            // 0.1より小さくなったら0にする
            if (current <= 0.1f)
            {
                normalAttackTimeIcon.fillAmount = 1f;
                normalAttackText.text = "";
            }
        }
    }


    /// <summary>
    /// フレーム変更
    /// </summary>
    public void SkillFrameChange(int index)
    {
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
    public void FrameColorChange(int index)
    {

        for (int i = 0; i < skillBlocks.Length; i++)
        {
            if (i < skillBlocks.Length && skillBlocks[i] != null)
            {
                Image blockImage = skillBlocks[i].GetComponent<Image>();
                if (blockImage != null)
                {
                    blockImage.color = playerColors[index];
                }
            }
        }
    }
}

