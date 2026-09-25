using NaughtyAttributes;
using NPOI.SS.UserModel;
using UnityEngine;

/// <summary>
/// スキルオブジェクトを生成するためのクラス
/// </summary>

[CreateAssetMenu(fileName = "NewSkill", menuName = "ScriptableObjects/SkillData")]
public class Skill : ScriptableObject
{
    [Label("スキル番号")][SerializeField] int skillNo;
    [Label("スキル名")][SerializeField] string skillName;
    [Label("スキル説明")][SerializeField] string skillEx;
    [Label("スキル画像")][SerializeField] Sprite skillSp;
    [Label("ターゲット方法")][SerializeField] AimSelect target;
    [Label("攻撃方法")][SerializeField] SkillCategory sCategory;
    [Label("攻撃値")][SerializeField] float atk;
    [Label("持続時間")][SerializeField] float keepTime;
    [Label("待機時間")][SerializeField] float delayTime;
    [Label("クールタイム")][SerializeField] float coolTime;
    [Label("移動速度")][SerializeField] float moveSpeed;
    [Label("貫通")][SerializeField] bool isPenetrate;
    [Label("反射回数")][SerializeField] int reflectCount;
    [Label("スキルオブジェクト")][SerializeField] GameObject effectAnimation;
    [Label("効果音")][SerializeField] SeName se;
    [Label("付与するエフェクト")][SerializeField] EffectAbility effect;
    [Label("弾数")][SerializeField] int bulletCount = 1;
    [Label("拡散角度")][SerializeField] float spreadAngle = 0f;
    [Label("挙動")][SerializeField] SkillBehaviorType skillBehavior;

    /// <summary>
    /// Excelで入力した値を代入
    /// </summary>
    public void LoadFromExcel(IRow row, IFormulaEvaluator evaluator)
    {
        //ヘルパー関数でロードを簡略化
        string Get(int i) => LoadManager.Instance.GetCellValueCalculated(row.GetCell(i), evaluator);
        var lm = LoadManager.Instance;

        //数値・Enumの変換
        this.skillNo = lm.ParseValue<int>(Get(0));
        this.skillName = Get(1);
        this.target = lm.ParseValue<AimSelect>(Get(2));
        this.sCategory = lm.ParseValue<SkillCategory>(Get(3));
        this.atk = lm.ParseValue<float>(Get(4));
        this.coolTime = lm.ParseValue<float>(Get(5));
        this.keepTime = lm.ParseValue<float>(Get(6));
        this.delayTime = lm.ParseValue<float>(Get(7));
        this.moveSpeed = lm.ParseValue<float>(Get(8));
        this.isPenetrate = (Get(9) == "1"); //1がTrue
        this.reflectCount = lm.ParseValue<int>(Get(10));
        this.skillEx = Get(18);
        this.bulletCount = lm.ParseValue<int>(Get(19));
        this.spreadAngle = lm.ParseValue<float>(Get(20));
        this.skillBehavior = lm.ParseValue<SkillBehaviorType>(Get(21));

        //Effectの変換
        if (System.Enum.TryParse(Get(14), true, out EffectList targetEnum))
        {
            this.effect = new EffectAbility(
                EffectRegistry.Get(targetEnum, lm.ParseValue<int>(Get(15)) == 1),
                true,
                lm.ParseValue<float>(Get(16)),
                lm.ParseValue<float>(Get(17)));
            Debug.Log("見つけたよ");
        }

        //リソースロード（パスが空ならnullを代入）
        this.skillSp = !string.IsNullOrEmpty(Get(11)) ? Resources.Load<Sprite>(Get(11)) : null;
        this.effectAnimation = !string.IsNullOrEmpty(Get(12)) ? Resources.Load<GameObject>(Get(12)) : null;
        string seNameStr = Get(13); // 13列目がSEの名前（文字列）であると仮定
        if (!string.IsNullOrEmpty(seNameStr) && System.Enum.TryParse<SeName>(seNameStr, true, out SeName parsedSeName))
            this.se = parsedSeName; // 型が SeName になったのでそのまま代入できる
        else
            this.se = SeName.num; 
    }

    /**
     * --------- ゲッター ---------
     */
    public int GetSkillNo() { return skillNo; }
    public string GetSkillName() { return skillName; }
    public string GetSkillEx() { return skillEx; }
    public Sprite GetSprite() { return skillSp; }
    public AimSelect GetAimSelect() { return target; }
    public SkillCategory GetSkillCategory() {  return sCategory; }
    public float GetAtk() { return atk; }
    public float GetKeepTime() {  return keepTime; }
    public float GetDelayTime() {  return delayTime; }
    public float GetCoolTime() {  return coolTime; }
    public float GetMoveSpeed() { return moveSpeed; }
    public bool GetIsPenetrate() { return isPenetrate; }
    public int GetReflectCount() { return reflectCount; }
    public GameObject GetEffectAnimation() {  return effectAnimation; }
    public SeName GetSe() { return se; }
    public EffectAbility GetEffect() {  return effect; }
    public SkillBehaviorType GetBehaviorType() { return skillBehavior; }

}

/// <summary>
/// スキルの攻撃方法
/// </summary>
public enum SkillCategory
{
    [InspectorName("通常攻撃")] Attack,
    [InspectorName("回復")] Heal,
    [InspectorName("バフ付与")] Effection
}


public enum SkillBehaviorType
{
    [InspectorName("未設定 / デフォルト（基本挙動）")] None,
    [InspectorName("直線（カーソルの方向へ飛ぶ）")] Straight,
    [InspectorName("通常（敵の位置や指定位置に直接出す）")] TargetPosition,
    [InspectorName("範囲攻撃（指定位置の周囲を巻き込む）")] Area,
    [InspectorName("設置型（その場に留まり続ける）")] Stationary,
    [InspectorName("地雷型（敵が近づくまで待機して爆発）")] Mine,
    [InspectorName("ホーミング（敵を追尾して曲がる）")] Homing,
    [InspectorName("戻ってくる（ブーメランのように手元に戻る）")] Boomerang,
    [InspectorName("連鎖型（敵から敵へピョンピョン跳ねる）")] Chain,
    [InspectorName("分裂型（途中でいくつかの小弾に分かれる）")] Split,
    [InspectorName("公転型（自分の周囲をぐるぐる回る）")] Orbit,
    [InspectorName("ノックバック特化（大きく吹き飛ばす）")] Knockback
}