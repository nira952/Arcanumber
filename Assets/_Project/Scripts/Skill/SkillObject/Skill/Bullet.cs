using UnityEngine;

public class Bullet : MagicObject
{
    private float speed = 10f;  //スピード
    private float damage = 0.5f;
    /// <summary>
    /// 初期設定
    /// </summary>
    public override void Initialize(int charaNo, Skill skill, Vector2 pos)
    {
        this.haveCharaNo = charaNo;
        this.dmg = damage;
        //弾の移動速度などを設定
        SetMovement(true, speed, false, false);
    }

    protected override void OnHit(NetworkPlayer target)
    {
        //ターゲットにダメージを与える処理
        target.TakeDamage(dmg);

        //弾なら当たったら自分を消す
        Destroy(gameObject);
    }
}