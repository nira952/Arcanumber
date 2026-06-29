using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 各スクリプタブルオブジェクトのリスト化
/// </summary>
public static class AssetLoader
{
    // スキルの収集場所
    private const string SKILL_PATH = "ScriptableObject/Skill";
    // アルカナの収集場所
    private const string ARCANA_PATH = "ScriptableObject/Arcana";

    public static List<Skill> LoadAllSkills()
    {
        // Resources/ScriptableObject/Skill 以下の全Skillをロード
        return new List<Skill>(Resources.LoadAll<Skill>(SKILL_PATH));
    }

    public static List<Arcana> LoadAllArcanas()
    {
        // Resources/ScriptableObject/Arcana 以下の全Arcanaをロード
        return new List<Arcana>(Resources.LoadAll<Arcana>(ARCANA_PATH));
    }
}
