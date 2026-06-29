using System.Collections;
using UnityEngine;

/// <summary>
/// アルカナの効果ロジックを表すインターフェース
/// </summary>
public abstract class ArcanaLogic
{
    public abstract ASkillCategory GetCategory();
    /// <summary>
    /// アニメーションやSEの再生
    /// </summary>
    protected void PlayArcanaVisuals(NetworkPlayer player, Arcana sourceArcana, Transform pos)
    {
        if (sourceArcana.effectPrefab != null)
        {
            //オブジェクトを出す場所とオブジェクトの親
            GameObject effectObj = Object.Instantiate(sourceArcana.effectPrefab, pos.position, Quaternion.identity);
            effectObj.transform.SetParent(player.transform);

            Animator animator = effectObj.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                //アニメーションの長さでオブジェクトを消す
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                Object.Destroy(effectObj, stateInfo.length);
            }
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
    public abstract void Execute(NetworkPlayer player, Arcana sourceArcana); 
    public virtual IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        yield break;
    }
}
