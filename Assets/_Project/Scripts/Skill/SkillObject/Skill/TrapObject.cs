using UnityEngine;

/// <summary>
/// トラップスキルのクラス
/// </summary>
public class TrapObject : MagicObject
{
    protected override void OnHit(NetworkPlayer target)
    {
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(haveCharaNo);
        //ダメージを与える
        PlayerUtility.FinalDamage(target, player, dmg);

        //トラップ固有のアニメーション再生
        if (animator != null)
        {
            animator.Play("Trap");
        }
    }
}