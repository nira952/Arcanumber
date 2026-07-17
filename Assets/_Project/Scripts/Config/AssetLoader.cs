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

    public List<Arcana> LoadAllArcanas => arcanaList;

    public List<Skill> LoadAllSkills => skillList;
    public List<EffectAbility> effectAbilities => effectList;

}
