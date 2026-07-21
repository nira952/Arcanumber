// ========================================================
// 死神：Death
// ========================================================

using UnityEngine;

/// <summary>
/// 死神（正位置）
/// </summary>
public class Arcana13DeathFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.SkillEffect;
    //攻撃を降るたびにダメージを受け、攻撃力を上げる
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        player.TakeDamage(sourceArcana.GetKeepValue());
        player.GetPlayerStatus().SetAtk(player.GetPlayerStatus().GetAtk() * 1.05f);
    }
}

/// <summary>
/// 死神（逆位置）
/// </summary>
public class Arcana13DeathBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //攻撃が当たるたびに回復する
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.AtkHeal, true);
        EffectAbility ea = new EffectAbility(e, false, -1f, -1f);
        player.SetHaveEffect(ea.Clone());
    }
}

