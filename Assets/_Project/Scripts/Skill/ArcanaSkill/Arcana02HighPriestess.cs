// ========================================================
// 女教皇：HighPriestess
// ========================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// 女教皇（正位置）
/// </summary>
public class Arcana02HighPriestessFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //だんだん攻撃力が上がる
    public const float timeInterval = 5f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana) { player.StartCoroutine(OnUpdate(player, sourceArcana)); }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        while (true)
        {
            yield return new WaitForSeconds(timeInterval);
            player.GetPlayerStatus().SetAtk(player.GetPlayerStatus().GetAtk() * sourceArcana.GetKeepValue());
        }
    }
}

/// <summary>
/// 女教皇（逆位置）
/// </summary>
public class Arcana02HighPriestessBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //だんだん攻撃力が下がる
    public const float normalAtkValue = 10f;
    public const float timeInterval = 5f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        //最初に攻撃力を上げる
        player.GetPlayerStatus().SetAtk(normalAtkValue);
        player.StartCoroutine(OnUpdate(player, sourceArcana));
    }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        //時間経過で攻撃力を下げる
        while (true)
        {
            yield return new WaitForSeconds(timeInterval);
            player.GetPlayerStatus().SetAtk(player.GetPlayerStatus().GetAtk() * sourceArcana.GetKeepValue());
        }
    }
}
