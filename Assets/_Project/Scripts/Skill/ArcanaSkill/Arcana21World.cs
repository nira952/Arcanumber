using UnityEngine;

// ========================================================
// 世界：World
// ========================================================

/// <summary>
/// 世界（正位置）
/// </summary>
public class Arcana21WorldFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //二段ジャンプができる
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        player.GetPlayerStatus().SetMaxJump(player.GetPlayerStatus().GetMaxJump() + 1);
    }
}

/// <summary>
/// 世界（逆位置）
/// </summary>
public class Arcana21WorldBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //次元移動ができる
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.WallSwap, true);
        EffectAbility ea = new EffectAbility(e, false, -1f, 0f);
        player.SetHaveEffect(ea.Clone());
    }
}
