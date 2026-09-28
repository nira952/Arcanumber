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
    //回復をスティールする効果
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.HealSteal, true);
        EffectAbility ea = new EffectAbility(e, false, -1f, -1f);
        player.AddEffect(ea.Clone());
    }
}

/// <summary>
/// 隠者（逆位置）
/// </summary>
public class Arcana09HermitBack : ArcanaLogic
{
    //トラップ設置
    public override void Execute(PlayerRoot player, Arcana sourceArcana) { player.StartCoroutine(OnUpdate(player, sourceArcana)); }
    public override IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
    {
        while (true)
        {
            GameObject trap = GameObject.Instantiate(
                sourceArcana.GetEffectPrefab());
            trap.transform.position = player.transform.position;
            yield return new WaitForSeconds(sourceArcana.GetKeepValue());
        }
    }
}
