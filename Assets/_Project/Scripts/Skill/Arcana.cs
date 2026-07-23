using NaughtyAttributes;
using NPOI.SS.UserModel;
using UnityEngine;

/// <summary>
/// アルカナを作るためのクラス
/// </summary>
[CreateAssetMenu(fileName = "NewArcana", menuName = "ScriptableObjects/ArcanaData")]
public class Arcana : ScriptableObject
{
    [ReadOnly][Label("アルカナ")][SerializeField] ArcanaList aList;
    [ReadOnly][Label("アルカナの画像")][SerializeField] Sprite arcanaImage;
    [ReadOnly][Label("正位置かどうか")][SerializeField] bool isFront;
    [ReadOnly][Label("アルカナスキルの説明")][TextArea(3, 10)][SerializeField] string arcanaEx;
    [ReadOnly][Label("発動条件")][SerializeField] ASkillCategory aCategory;
    [ReadOnly][Label("クールタイム")][SerializeField] float coolTime;
    [ReadOnly][Label("持続数値")][SerializeField] float keepValue;
    [ReadOnly][Label("エフェクト")]public GameObject effectPrefab;
    [ReadOnly][Label("効果音")]public AudioClip se;

    //アルカナの効果を入れるためのクラス
    private ArcanaLogic arcanaLogic;

    /// <summary>
    /// アルカナに数値を入れるメソッド
    /// </summary>
    public void LoadFromExcel(IRow row, IFormulaEvaluator evaluator)
    {
        //ラムダ式で呼び出しを短縮
        string Get(int i) => LoadManager.Instance.GetCellValueCalculated(row.GetCell(i), evaluator);
        var lm = LoadManager.Instance;

        this.aList = lm.ParseValue<ArcanaList>(Get(0));
        this.isFront = (lm.ParseValue<int>(Get(1)) == 1);
        this.aCategory = lm.ParseValue<ASkillCategory>(Get(3));
        this.coolTime = lm.ParseValue<float>(Get(4));
        this.keepValue = lm.ParseValue<float>(Get(5));

        //リソース系
        this.effectPrefab = !string.IsNullOrEmpty(Get(6)) ? Resources.Load<GameObject>(Get(6)) : null;
        this.se = !string.IsNullOrEmpty(Get(7)) ? Resources.Load<AudioClip>(Get(7)) : null;
        this.arcanaEx = Get(8);

        //ロジック更新
        this.arcanaLogic = CreateInstanceFromName(Get(9));
        if (arcanaLogic != null)
            this.aCategory = arcanaLogic.GetCategory();
    }

    /// <summary>
    /// ArcanaLogicを探す処理
    /// </summary>
    private ArcanaLogic CreateInstanceFromName(string className)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type type = assembly.GetType(className);
            if (type != null && typeof(ArcanaLogic).IsAssignableFrom(type))
            {
                return (ArcanaLogic)System.Activator.CreateInstance(type);
            }
        }
        Debug.LogError($"【エラー】クラス名 '{className}' が見つからないか、ArcanaLogicを継承していません！");
        return null;
    }

    /// <summary>
    /// アルカナ発動
    /// </summary>
    public void ExecuteArcanaEffect(ASkillCategory currentCategory, NetworkPlayer player)
    {
        // それでもnullなら諦める
        if (arcanaLogic == null) return;

        if (aCategory == currentCategory)
            arcanaLogic.Execute(player, this);

        //Debug.Log($"【アルカナ発動】{GetArcanaName}の効果が {currentCategory} のタイミングで発動しました。");
    }

    /**
     * --------- ゲッター ---------
     */
    public string GetArcanaName => aList switch
    {
        ArcanaList.Fool => "愚者",
        ArcanaList.Magician => "魔術師",
        ArcanaList.HighPriestess => "女教皇",
        ArcanaList.Empress => "女帝",
        ArcanaList.Emperor => "皇帝",
        ArcanaList.Hierophant => "教皇",
        ArcanaList.Lovers => "恋人",
        ArcanaList.Chariot => "戦車",
        ArcanaList.Strength => "力",
        ArcanaList.Hermit => "隠者",
        ArcanaList.WheelOfFortune => "運命の輪",
        ArcanaList.Justice => "正義",
        ArcanaList.HangedMan => "つるされた男",
        ArcanaList.Death => "死神",
        ArcanaList.Temperance => "節制",
        ArcanaList.Devil => "悪魔",
        ArcanaList.Tower => "塔",
        ArcanaList.Star => "星",
        ArcanaList.Moon => "月",
        ArcanaList.Sun => "太陽",
        ArcanaList.Judgement => "審判",
        ArcanaList.World => "世界",
        _ => aList.ToString()
    };
    public int GetArcanaListID() => (int)aList;
    public Sprite GetArcanaImage() => arcanaImage;
    public bool GetIsFront() => isFront;
    public string GetArcanaEX() => arcanaEx;
    public ASkillCategory GetASkillCategory() => aCategory;
    public float GetCoolTime() => coolTime;
    public float GetKeepValue() => keepValue;
    public GameObject GetEffectPrefab() => effectPrefab;
    public Animator GetAnimation() => effectPrefab != null ? effectPrefab.GetComponent<Animator>() : null;
    public AudioClip GetSE() => se;


    /**
    * --------- セッター ---------
    */

    public void SetArcanaEx(string arcanaEx) => this.arcanaEx = arcanaEx;
    public void SetASkillCategory(ASkillCategory category) { aCategory = category; }

}

/// <summary>
/// アルカナの種類
/// </summary>
public enum ArcanaList
{
    [InspectorName("愚者")]Fool = 0,
    [InspectorName("魔術師")] Magician = 1,
    [InspectorName("女教皇")] HighPriestess = 2,
    [InspectorName("女帝")] Empress = 3,
    [InspectorName("皇帝")] Emperor = 4,
    [InspectorName("教皇")] Hierophant = 5,
    [InspectorName("恋人")] Lovers = 6,
    [InspectorName("戦車")] Chariot = 7,
    [InspectorName("力")] Strength = 8,
    [InspectorName("隠者")] Hermit = 9,
    [InspectorName("運命の輪")] WheelOfFortune = 10,
    [InspectorName("正義")] Justice = 11,
    [InspectorName("つるされた男")] HangedMan = 12,
    [InspectorName("死神")] Death = 13,
    [InspectorName("節制")] Temperance = 14,
    [InspectorName("悪魔")] Devil = 15,
    [InspectorName("塔")] Tower = 16,
    [InspectorName("星")] Star = 17,
    [InspectorName("月")] Moon = 18,
    [InspectorName("太陽")] Sun = 19,
    [InspectorName("審判")] Judgement = 20,
    [InspectorName("世界")] World = 21
}

/// <summary>
/// スキルの発動条件
/// </summary>
public enum ASkillCategory
{
    [InspectorName("スタート時に発動")] StartEffect,
    [InspectorName("ボタンを押すことで発動")] Command,
    [InspectorName("スキル発動に合わせて発動")] SkillEffect,
    [InspectorName("ダメージを受けたときに発動")] DamageEffect,
    [InspectorName("死亡時に発動")] DeathEffect
}
