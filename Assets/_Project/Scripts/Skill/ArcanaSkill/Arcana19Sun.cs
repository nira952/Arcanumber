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
        Vector2 pos = player.GetAimCursor().GetEfeUpperPos().position;
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, false,
            (int)EffectList.SunBurn, false, false, sourceArcana.GetKeepValue(), numValue);
        ArcanaNetworkManager.Instance.SetAnimation(player.PlayerIndex.Value, pos, sourceArcana.GetKeepValue());
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
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
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, false,
            (int)EffectList.Stun, false, false, sourceArcana.GetKeepValue(), -1);
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
}
