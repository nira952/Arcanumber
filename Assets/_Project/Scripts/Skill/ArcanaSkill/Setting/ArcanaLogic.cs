using System.Collections;
using UnityEngine;

/// <summary>
/// アルカナの効果ロジックを表すインターフェース
/// </summary>
public abstract class ArcanaLogic
{
    /// <summary>
    /// アニメーションやSEの再生
    /// </summary>
    protected void PlayArcanaVisuals(PlayerRoot player, Arcana sourceArcana, Transform pos)
    {
        if (sourceArcana.effectPrefab != null)
        {
            ArcanaNetworkManager.Instance.SetData(player.PlayerIndex.Value, pos.position);
        }
    }
    /// <summary>
    /// エフェクトの再生時間を取得する
    /// </summary>
    protected float GetVisualDuration(Arcana sourceArcana)
    {
        if (sourceArcana.effectPrefab == null) return 0f;

        Animator anim = sourceArcana.effectPrefab.GetComponentInChildren<Animator>();
        if (anim == null) return 0f;

        //現在のステートの長さを返す
        return anim.GetCurrentAnimatorStateInfo(0).length;
    }
    public abstract void Execute(PlayerRoot player, Arcana sourceArcana); 
    public virtual IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
    {
        yield break;
    }
}
