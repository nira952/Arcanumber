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
        ArcanaNetworkManager.Instance.SetStatus(player.PlayerIndex.Value, StatusCategory.Atk, player.GetPlayerStatus().GetAtk() * 1.05f);
        ArcanaNetworkManager.Instance.SetAnimation(player.PlayerIndex.Value, player.transform.position, 0.5f);
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
        Vector2 pos = player.GetAimCursor().GetEfeUpperPos().position;
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, true,
            (int)EffectList.AtkHeal, true, false, -1f, -1f);
        ArcanaNetworkManager.Instance.SetAnimation(player.PlayerIndex.Value, pos, 0.5f);
    }
}

