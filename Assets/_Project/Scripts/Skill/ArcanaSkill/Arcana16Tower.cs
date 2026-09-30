// ========================================================
// 塔：Tower
// ========================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// 塔（正位置）
/// </summary>
public class Arcana16TowerFront : ArcanaLogic
{
    //永続するタレットがおける
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        SkillManager.Instance.SpawnRpcObject(player.PlayerIndex.Value);
    }
}

/// <summary>
/// 塔（逆位置）
/// </summary>
public class Arcana16TowerBack : ArcanaLogic
{
    //５０％の確率で同じスキルが発動する
    private float time = 3f;
    public override void Execute(PlayerRoot player, Arcana sourceArcana) { player.StartCoroutine(OnUpdate(player, sourceArcana)); }
    public override IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
    {
        if (Random.value <= sourceArcana.GetKeepValue())
        {
            yield return new WaitForSeconds(time);

            // SkillManagerにスキル発動を依頼する
            SkillManager.Instance.RequestSkill(player);
        }
    }
}
