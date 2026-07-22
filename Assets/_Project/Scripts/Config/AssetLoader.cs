using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 各スクリプタブルオブジェクトのリスト化
/// </summary>
public class AssetLoader : SingletonMonoBehaviour<AssetLoader>
{
    private List<Arcana> arcanaList = new List<Arcana>();
    private List<Skill> skillList = new List<Skill>();
    private List<EffectAbility> effectList = new List<EffectAbility>();

    public void SetArcanaList(List<Arcana> arcanaList) => this.arcanaList = arcanaList;
    public void SetSkillList(List<Skill> skillList) => this.skillList = skillList;
    public void SetEffectList(List<EffectAbility> effectList) => this.effectList = effectList;

    public List<Arcana> LoadAllArcanas => arcanaList;
    public List<Skill> LoadAllSkills => skillList;
    public List<EffectAbility> effectAbilities => effectList;


    /// <summary>
    /// 条件に合うアルカナを探す
    /// </summary>
    public Arcana GetArcana(ArcanaList no, bool front)
    {
        return arcanaList.FirstOrDefault(e => (ArcanaList)e.GetArcanaListID() == no
            && e.GetIsFront() == front);
    }

    /// <summary>
    /// 条件に合うスキルを探す
    /// </summary>
    public Skill GetSkill(int skillNo)
    {
        return skillList.FirstOrDefault(e => e.GetSkillNo() == skillNo);
    }
}
