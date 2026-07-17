using UnityEngine;

public class NormalArrow : MagicObject
{
    public override void Initialize(int charaNo, Skill skill, Vector2 pos)
    {
        base.Initialize(charaNo, skill, pos);
        // 最後に位置と回転を確定させる
        SetupPositionAndRotation(pos);
    }
    protected override void OnHit(NetworkPlayer target)
    {
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(haveCharaNo);
        //ダメージを与える
        PlayerUtility.FinalDamage(target, player, dmg);
        //エフェクトをつける
        if (effect != null)
            target.SetHaveEffect(effect.Clone());
        //消す
        Destroy(gameObject);
    }
}
