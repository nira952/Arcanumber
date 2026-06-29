using System.Collections.Generic;

// ========================================================
// 正義：Justice
// ========================================================

/// <summary>
/// 正義（正位置）
/// </summary>
public class Arcana11JusticeFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //全員にダメージを与える
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        List<NetworkPlayer> list = PlayerUtility.GetOtherPlayers(player);
        foreach (NetworkPlayer p in list)
            p.TakeDamage(15);
    }
}

/// <summary>
/// 正義（逆位置）
/// </summary>
public class Arcana11JusticeBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //半分のダメージを返す（カウンター）
    private int timeValue = 10;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.Counter, true);
        EffectAbility ea = new EffectAbility(e, true, timeValue, -1f);
        player.SetHaveEffect(ea.Clone());
    }
}
