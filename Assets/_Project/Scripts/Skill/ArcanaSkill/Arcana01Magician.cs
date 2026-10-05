// ========================================================
// 魔術師：Magician
// ========================================================

using UnityEngine;

/// <summary>
/// 魔術師（正位置）
/// </summary>
public class Arcana01MagicianFront : ArcanaLogic
{
    //防御無視
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetEffect
            (player.PlayerIndex.Value, true, 
            (int)EffectList.IgnoreDefense, true, false, -1f, -1f);
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
}

/// <summary>
/// 魔術師（逆位置）
/// </summary>
public class Arcana01MagicianBack : ArcanaLogic
{
    //自分のサポートをしてくれる敵を召喚する
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
        for (int i = 0; i < (int)sourceArcana.GetKeepValue(); i++)
        {
            SkillManager.Instance.SpawnRpcObject(player.PlayerIndex.Value);
        }
    }
}
