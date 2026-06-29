// ========================================================
// 戦車：Chariot
// ========================================================

using UnityEngine;

/// <summary>
/// 戦車（正位置）
/// </summary>
public class Arcana07ChariotFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //速度が上がる
    public const float speedValue = 1.5f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        float newSpeed = player.GetPlayerStatus().GetSpeed() * speedValue;
        player.GetPlayerStatus().SetSpeed(newSpeed);
    }
}

/// <summary>
/// 戦車（逆位置）
/// </summary>
public class Arcana07ChariotBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //スキル発動が50％速くなる
    public const float skillSpeedUp = 1f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        EffectAbility effect = new EffectAbility(
            EffectRegistry.Get(EffectList.SkillTimeReduction, true),
            false, -1, skillSpeedUp);
        player.SetHaveEffect(effect.Clone());
    }
}


