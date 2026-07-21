using NUnit.Framework.Internal;
using UnityEngine;

public class NormalArrow : SkillObject
{
    protected override void OnHit(NetworkPlayer target)
    {
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(haveCharaNo);

        // ダメージを与える
        PlayerUtility.FinalDamage(target, player, dmg);

        // エフェクトをつける
        if (effect != null)
            target.SetHaveEffect(effect.Clone());
    }
}