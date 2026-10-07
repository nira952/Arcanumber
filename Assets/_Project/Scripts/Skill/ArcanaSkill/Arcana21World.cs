using UnityEngine;

// ========================================================
// 世界：World
// ========================================================

/// <summary>
/// 世界（正位置）
/// </summary>
public class Arcana21WorldFront : ArcanaLogic
{
    //二段ジャンプができる
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetJump(player.PlayerIndex.Value, 1);
    }
}

/// <summary>
/// 世界（逆位置）
/// </summary>
public class Arcana21WorldBack : ArcanaLogic
{
    //次元移動ができる
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, true,
            (int)EffectList.WallSwap, true, false, -1f, 0f);
    }
}
