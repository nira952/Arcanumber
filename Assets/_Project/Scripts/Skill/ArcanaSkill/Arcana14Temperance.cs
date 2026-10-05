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
    //自分のスキルのクールタイムを減らす
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, true,
            (int)EffectList.CoolTimeReduction, true, false, -1f, sourceArcana.GetKeepValue());
    }
}

/// <summary>
/// 節制（逆位置）
/// </summary>
public class Arcana14TemperanceBack : ArcanaLogic
{
    //相手のスキルのクールタイムを増やす
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, false,
            (int)EffectList.CoolTimeReduction, false, false, -1f, sourceArcana.GetKeepValue());
    }
}