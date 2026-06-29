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
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //永続するタレットがおける
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        GameObject turret = Object.Instantiate(sourceArcana.effectPrefab);
        turret.transform.position = player.transform.position;
        Turret t = turret.GetComponent<Turret>();
        t.SetHaveNo(player.GetNetworkId());
    }
}

/// <summary>
/// 塔（逆位置）
/// </summary>
public class Arcana16TowerBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.SkillEffect;
    //５０％の確率で同じスキルが発動する
    private float time = 3f;
    private float success = 0.5f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana) { player.StartCoroutine(OnUpdate(player, sourceArcana)); }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        int sNo = player.GetSkillNo();
        if (Random.value <= success)
        {
            yield return new WaitForSeconds(time);
            SkillManager.Instance.RequestSkill(player, sNo);
        }
    }
}
