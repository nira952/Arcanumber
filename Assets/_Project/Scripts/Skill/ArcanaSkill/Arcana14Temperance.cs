// ========================================================
// 節制：Temperance
// ========================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 節制（正位置）
/// </summary>
public class Arcana14TemperanceFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //自分のスキルのクールタイムを減らす
    private float efeValue = 0.2f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.CoolTimeReduction, true);
        EffectAbility ea = new EffectAbility(e, false, -1f, efeValue);
        //自分にかける
        player.SetHaveEffect(ea.Clone());
    }
}

/// <summary>
/// 節制（逆位置）
/// </summary>
public class Arcana14TemperanceBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //相手のスキルのクールタイムを増やす
    private float efeValue = 0.2f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.CoolTimeReduction, false);
        EffectAbility ea = new EffectAbility(e, false, -1f, efeValue);
        //自分以外にかける
        PlayerUtility.ApplyEffectToOthers(player, ea);
    }
}