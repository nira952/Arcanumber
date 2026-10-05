using System.Collections.Generic;
using UnityEngine;

// ========================================================
// 正義：Justice
// ========================================================

/// <summary>
/// 正義（正位置）
/// </summary>
public class Arcana11JusticeFront : ArcanaLogic
{
    //全員にダメージを与える
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetDamage(player.PlayerIndex.Value, true, sourceArcana.GetKeepValue());
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
}

/// <summary>
/// 正義（逆位置）
/// </summary>
public class Arcana11JusticeBack : ArcanaLogic
{
    //半分のダメージを返す（カウンター）
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, true,
            (int)EffectList.Counter, true, true, sourceArcana.GetKeepValue(), -1f);

        Vector2 pos = player.transform.position + sourceArcana.GetEffectPrefab().transform.position;
        ArcanaNetworkManager.Instance.SetAnimation(player.PlayerIndex.Value, pos, sourceArcana.GetKeepValue());
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
}
