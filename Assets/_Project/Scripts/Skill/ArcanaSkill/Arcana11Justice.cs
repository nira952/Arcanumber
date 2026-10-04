using System.Collections.Generic;

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
        List<PlayerRoot> list = PlayerUtility.GetOtherPlayers(player);
        foreach (PlayerRoot p in list)
            p.ApplyDamage(sourceArcana.GetKeepValue());
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
        Effect e = EffectRegistry.Get(EffectList.Counter, true);
        EffectAbility ea = new EffectAbility(e, true, sourceArcana.GetKeepValue(), -1f);
        player.AddEffect(ea.Clone());
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
}
