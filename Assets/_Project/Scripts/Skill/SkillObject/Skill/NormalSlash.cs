using UnityEngine;

public class NormalSlash : SkillObject
{
    private void Start()
    {
        // スキルの初期化処理
        isHitAndWaitingDestroy = false; // ヒット後にオブジェクトを破棄しないフラグを設定
    }

    protected override void OnHit(NetworkPlayer target)
    {
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(haveCharaNo);
        //ダメージを与える
        PlayerUtility.FinalDamage(target, player, dmg);
        // エフェクトをつける
        if (effect != null)
            target.SetHaveEffect(effect.Clone());
    }
}
