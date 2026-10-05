// ========================================================
// 死神：Death
// ========================================================

using UnityEngine;

/// <summary>
/// 死神（正位置）
/// </summary>
public class Arcana13DeathFront : ArcanaLogic
{
    //攻撃を降るたびにダメージを受け、攻撃力を上げる
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetDamage(player.PlayerIndex.Value, false, sourceArcana.GetKeepValue());
        ArcanaNetworkManager.Instance.SetStatus(player.PlayerIndex.Value, StatusCategory.Atk, 1.05f);
    }
}

/// <summary>
/// 死神（逆位置）
/// </summary>
public class Arcana13DeathBack : ArcanaLogic
{
    //攻撃が当たるたびに回復する
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, true,
            (int)EffectList.AtkHeal, true, false, -1f, -1f);
    }
}

