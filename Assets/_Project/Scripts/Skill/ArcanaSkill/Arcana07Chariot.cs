// ========================================================
// 戦車：Chariot
// ========================================================

using UnityEngine;

/// <summary>
/// 戦車（正位置）
/// </summary>
public class Arcana07ChariotFront : ArcanaLogic
{
    //速度が上がる
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetStatus(player.PlayerIndex.Value, StatusCategory.Speed, player.GetPlayerStatus().GetSpeed() * sourceArcana.GetKeepValue());
    }
}

/// <summary>
/// 戦車（逆位置）
/// </summary>
public class Arcana07ChariotBack : ArcanaLogic
{
    //スキル発動が50％速くなる
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, true,
            (int)EffectList.SkillTimeReduction, true, false, -1, sourceArcana.GetKeepValue());
    }
}


