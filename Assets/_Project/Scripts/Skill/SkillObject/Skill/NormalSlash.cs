using UnityEngine;

public class NormalSlash : MagicObject
{
    public void Initialize(int charaNo, float damage, float lifetime = 1.0f)
    {
        CommonInitialize(charaNo, damage);
        isPenetrate = true;
        dmg = damage;
    }

    // プレイヤーに当たったときに呼ばれる（MagicObject側で重複ヒット防止済み）
    protected override void OnHit(NetworkPlayer target)
    {
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(haveCharaNo);
        if (player == null) return;

        //ダメージを与える（エフェクト付与はなし）
        PlayerUtility.FinalDamage(target, player, dmg);
        Debug.Log("当たったよ");
    }

    public void ResetList() => hitList.Clear();
}