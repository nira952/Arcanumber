using NaughtyAttributes;
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
    [Label("スキルオブジェクト")][SerializeField] GameObject effectAnimation;
    [Label("効果音")][SerializeField] AudioClip se;
    [Label("付与するエフェクト")][SerializeField] EffectAbility effect;

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
    public GameObject GetEffectAnimation() {  return effectAnimation; }
    public AudioClip GetSe() { return se; }
    public EffectAbility GetEffect() {  return effect; }

}

/// <summary>
/// スキルの攻撃方法
/// </summary>
public enum SkillCategory
{
    [InspectorName("通常攻撃")] Attack,
    [InspectorName("回復")] Heal,
    [InspectorName("バフ付与")] EffectionBuff
}
