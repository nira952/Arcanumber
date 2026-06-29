using System.Collections;
using UnityEngine;

// ========================================================
// 隠者：Hermit
// ========================================================

/// <summary>
/// 隠者（正位置）
/// </summary>
public class Arcana09HermitFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //回復をスティールする効果
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.HealSteal, true);
        EffectAbility ea = new EffectAbility(e, false, -1f, -1f);
        player.SetHaveEffect(ea.Clone());
    }
}

/// <summary>
/// 隠者（逆位置）
/// </summary>
public class Arcana09HermitBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //トラップ設置
    private const float timeInterval = 15f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana) { player.StartCoroutine(OnUpdate(player, sourceArcana)); }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        while (true)
        {
            GameObject trap = GameObject.Instantiate(
                sourceArcana.GetEffectPrefab());
            trap.transform.position = player.transform.position;
            yield return new WaitForSeconds(timeInterval);
        }
    }
}
