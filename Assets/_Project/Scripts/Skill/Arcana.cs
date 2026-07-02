using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// アルカナを作るためのクラス
/// </summary>
[CreateAssetMenu(fileName = "NewArcana", menuName = "ScriptableObjects/ArcanaData")]
public class Arcana : ScriptableObject
{
    [Label("アルカナ")][SerializeField] ArcanaList aList;
    [Label("アルカナの画像")][SerializeField] Sprite arcanaImage;
    [Label("正位置かどうか")][SerializeField] bool isFront;
    [Label("アルカナスキルの説明")][TextArea(3, 10)][SerializeField] string arcanaEx;
    [ReadOnly][Label("発動条件")][SerializeField] ASkillCategory aCategory;
    [Label("クールタイム")][SerializeField] float coolTime;
    [Label("エフェクト")]public GameObject effectPrefab;
    [Label("効果音")]public AudioClip se;

    //アルカナの効果を入れるためのクラス
    private ArcanaLogic arcanaLogic;

    private void OnValidate()
    {
        //アルカナの効果を変える
        SetupLogic();
        //自動的にカテゴリーを入れる
        if (arcanaLogic != null)
            aCategory = arcanaLogic.GetCategory();
    }

    /// <summary>
    /// アルカナカテゴリーからロジックを入れる
    /// </summary>
    private void SetupLogic()
    {
        arcanaLogic = (aList, isFront) switch
        {
            //愚者
            (ArcanaList.Fool, true) => new Arcana00FoolFront(),
            (ArcanaList.Fool, false) => new Arcana00FoolBack(),
            //魔術師
            (ArcanaList.Magician, true) => new Arcana01MagicianFront(),
            (ArcanaList.Magician, false) => new Arcana01MagicianBack(),
            //女教皇
            (ArcanaList.HighPriestess, true) => new Arcana02HighPriestessFront(),
            (ArcanaList.HighPriestess, false) => new Arcana02HighPriestessBack(),
            //女帝
            (ArcanaList.Empress, true) => new Arcana03EmpressFront(),
            (ArcanaList.Empress, false) => new Arcana03EmpressBack(),
            //皇帝
            (ArcanaList.Emperor, true) => new Arcana04EmperorFront(),
            (ArcanaList.Emperor, false) => new Arcana04EmperorBack(),
            //教皇
            (ArcanaList.Hierophant, true) => new Arcana05HierophantFront(),
            (ArcanaList.Hierophant, false) => new Arcana05HierophantBack(),
            //恋人
            (ArcanaList.Lovers, true) => new Arcana06LoversFront(),
            (ArcanaList.Lovers, false) => new Arcana06LoversBack(),
            //戦車
            (ArcanaList.Chariot, true) => new Arcana07ChariotFront(),
            (ArcanaList.Chariot, false) => new Arcana07ChariotBack(),
            //力
            (ArcanaList.Strength, true) => new Arcana08StrengthFront(),
            (ArcanaList.Strength, false) => new Arcana08StrengthBack(),
            //隠者
            (ArcanaList.Hermit, true) => new Arcana09HermitFront(),
            (ArcanaList.Hermit, false) => new Arcana09HermitBack(),
            //運命の輪
            (ArcanaList.WheelOfFortune, true) => new Arcana10WheelOfFortuneFront(),
            (ArcanaList.WheelOfFortune, false) => new Arcana10WheelOfFortuneBack(),
            //正義
            (ArcanaList.Justice, true) => new Arcana11JusticeFront(),
            (ArcanaList.Justice, false) => new Arcana11JusticeBack(),
            //つるされた男
            (ArcanaList.HangedMan, true) => new Arcana12HangedManFront(),
            (ArcanaList.HangedMan, false) => new Arcana12HangedManBack(),
            //死神
            (ArcanaList.Death, true) => new Arcana13DeathFront(),
            (ArcanaList.Death, false) => new Arcana13DeathBack(),
            //節制
            (ArcanaList.Temperance, true) => new Arcana14TemperanceFront(),
            (ArcanaList.Temperance, false) => new Arcana14TemperanceBack(),
            //悪魔
            (ArcanaList.Devil, true) => new Arcana15DevilFront(),
            (ArcanaList.Devil, false) => new Arcana15DevilBack(),
            //塔
            (ArcanaList.Tower, true) => new Arcana16TowerFront(),
            (ArcanaList.Tower, false) => new Arcana16TowerBack(),
            //星
            (ArcanaList.Star, true) => new Arcana17StarFront(),
            (ArcanaList.Star, false) => new Arcana17StarBack(),

            _ => null
        };
    }

    /// <summary>
    /// アルカナ発動
    /// </summary>
    public void ExecuteArcanaEffect(ASkillCategory currentCategory, NetworkPlayer player)
    {
        //ロジックが入っているか確認    
        if (arcanaLogic == null) return;

        if (aCategory == currentCategory)
            arcanaLogic.Execute(player, this);

        Debug.Log($"【アルカナ発動】{GetArcanaName}の効果が {currentCategory} のタイミングで発動しました。");
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
    public GameObject GetEffectPrefab() => effectPrefab;
    public Animator GetAnimation() => effectPrefab != null ? effectPrefab.GetComponent<Animator>() : null;
    public AudioClip GetSE() => se;


    /**
    * --------- セッター ---------
    */
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
    [InspectorName("攻撃")] ATK,
    [InspectorName("スタート時に発動")] StartEffect,
    [InspectorName("時間で発動")] TimeEffect,
    [InspectorName("ボタンを押すことで発動")] Command,
    [InspectorName("スキル発動に合わせて発動")] SkillEffect,
    [InspectorName("ダメージを受けたときに発動")] DamageEffect,
    [InspectorName("死亡時に発動")] DeathEffect
}
