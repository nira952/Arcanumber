using System.Collections;
using UnityEngine;

// ========================================================
// 隠者：Hermit
// ========================================================

/// <summary>
/// 隠者（正位置）
/// </summary>
public class Arcana09HermitFront : ArcanaLogic
{
    //回復をスティールする効果
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        Vector2 pos = player.GetAimCursor().GetEfeUpperPos().position;
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, true,
            (int)EffectList.HealSteal, true, false, -1f, -1f);
        ArcanaNetworkManager.Instance.SetAnimation(player.PlayerIndex.Value, pos, 0.5f);
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
}

/// <summary>
/// 隠者（逆位置）
/// </summary>
public class Arcana09HermitBack : ArcanaLogic
{
    //トラップ設置
    public override void Execute(PlayerRoot player, Arcana sourceArcana) { player.StartCoroutine(OnUpdate(player, sourceArcana)); }
    public override IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
    {
        while (true)
        {
            //トラップを設置するロジックをここに記述
            ArcanaNetworkManager.Instance.SetAnimation(player.PlayerIndex.Value, player.transform.position, 60f);
            NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());

            yield return new WaitForSeconds(sourceArcana.GetKeepValue());
        }
    }
}
