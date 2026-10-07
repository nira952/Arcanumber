// ========================================================
// 星：Star
// ========================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// 星（正位置）
/// </summary>
public class Arcana17StarFront : ArcanaLogic
{
    //あなたはスターだ
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        player.StartCoroutine(OnUpdate(player, sourceArcana));
    }
    public override IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
    {
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
        VisualEffectManager.Instance.ShowZoomPlayer(player.PlayerIndex.Value, sourceArcana.GetKeepValue());

        yield return null;
    }
}

/// <summary>
/// 星（逆位置）
/// </summary>
public class Arcana17StarBack : ArcanaLogic
{
    //無敵
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, true,
            (int)EffectList.Invincible, true, true, sourceArcana.GetKeepValue(), -1f);
        ArcanaNetworkManager.Instance.SetAnimation(player.PlayerIndex.Value, player.transform.position, sourceArcana.GetKeepValue());
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }

}
