using UnityEngine;

/// <summary>
/// トラップスキルのクラス
/// </summary>
public class TrapObject : MagicObject
{
    protected override void OnHit(PlayerRoot target)
    {
        base.OnHit(target);

        //トラップ固有のアニメーション再生
        if (animator != null)
        {
            animator.Play("Trap");
        }
    }
}