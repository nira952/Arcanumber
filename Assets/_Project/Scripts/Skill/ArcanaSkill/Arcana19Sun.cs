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
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //晴れ日和
    private float keepTime = 15f;
    private float numValue = 0.5f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        List<NetworkPlayer> others = PlayerUtility.GetOtherPlayers(player);
        foreach (NetworkPlayer other in others)
        {
            Effect e = EffectRegistry.Get(EffectList.SunBurn, false);
            EffectAbility effect = new EffectAbility(e, false, keepTime, numValue);
            other.SetHaveEffect(effect);
        }
    }
}

/// <summary>
/// 太陽（逆位置）
/// </summary>
public class Arcana19SunBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //あなたはスターだ
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
    }
}
