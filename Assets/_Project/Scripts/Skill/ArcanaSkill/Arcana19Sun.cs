using System.Collections.Generic;
using UnityEngine;

// ========================================================
// 太陽：Sun
// ========================================================

/// <summary>
/// 太陽（正位置）
/// </summary>
public class Arcana19SunFront : ArcanaLogic
{
    //晴れ日和
    private float numValue = 0.5f;
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        List<PlayerRoot> others = PlayerUtility.GetOtherPlayers(player);
        foreach (PlayerRoot other in others)
        {
            Effect e = EffectRegistry.Get(EffectList.SunBurn, false);
            EffectAbility effect = new EffectAbility(e, false, sourceArcana.GetKeepValue(), numValue);
            other.AddEffect(effect);
        }
    }
}

/// <summary>
/// 太陽（逆位置）
/// </summary>
public class Arcana19SunBack : ArcanaLogic
{
    //スタンする
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        List<PlayerRoot> others = PlayerUtility.GetOtherPlayers(player);
        foreach (PlayerRoot other in others)
        {
            Effect e = EffectRegistry.Get(EffectList.Stun, false);
            EffectAbility effect = new EffectAbility(e, false, sourceArcana.GetKeepValue(), -1);
            other.AddEffect(effect);
        }
    }
}
