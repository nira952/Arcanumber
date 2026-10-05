// ========================================================
// 恋人：Lovers
// ========================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// 恋人（正位置）
/// </summary>
public class Arcana06LoversFront : ArcanaLogic
{
    //魅了状態にする
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, true,
            (int)EffectList.Charm, true, false, sourceArcana.GetKeepValue(), -1f);

        Vector2 pos = player.transform.position + sourceArcana.GetEffectPrefab().transform.position;
        ArcanaNetworkManager.Instance.SetAnimation(player.PlayerIndex.Value, pos, sourceArcana.GetKeepValue());
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
}

/// <summary>
/// 恋人（逆位置）
/// </summary>
public class Arcana06LoversBack : ArcanaLogic
{
    //画面を暗くする
    public override void Execute(PlayerRoot player, Arcana sourceArcana) { player.StartCoroutine(OnUpdate(player, sourceArcana)); }
    public override IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
    {
        //画面を暗くするエフェクトを再生
        VisualEffectManager.Instance.ShowDarkPanel(player.PlayerIndex.Value,sourceArcana.GetKeepValue());
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
        yield return null;
    }
}
