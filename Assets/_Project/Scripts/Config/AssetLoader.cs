using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 各スクリプタブルオブジェクトのリスト化
/// </summary>
public class AssetLoader : SingletonMonoBehaviour<AssetLoader>
{
    private List<Arcana> arcanaList = new List<Arcana>();   //アルカナを格納するリスト
    private List<Skill> skillList = new List<Skill>();  //スキルを格納するリスト

    /// <summary>
    /// アルカナを格納するリストをセット
    /// </summary>
    public void SetArcanaList(List<Arcana> arcanaList) => this.arcanaList = arcanaList;

    /// <summary>
    /// スキルを格納するリストをセット
    /// </summary>
    public void SetSkillList(List<Skill> skillList) => this.skillList = skillList;

    /// <summary>
    /// すべてのアルカナを取得する
    /// </summary>
    public List<Arcana> LoadAllArcanas => arcanaList;

    /// <summary>
    /// すべてのスキルを取得する
    /// </summary>
    public List<Skill> LoadAllSkills => skillList;


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
