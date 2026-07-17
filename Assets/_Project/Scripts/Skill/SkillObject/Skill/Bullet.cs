using UnityEngine;

public class Bullet : MagicObject
{
    private float speed = 10f;
    private float damage = 0.5f;

    //スキルとして登録しないのであれば、独自の初期化メソッド名でOK
    public void InitializeBullet(int charaNo, Vector2 pos)
    {
        //親の初期化を呼び出す
        base.Initialize(charaNo, pos, damage, speed, false, 0);
    }

    protected override void OnHit(NetworkPlayer target)
    {
        target.TakeDamage(dmg);
        Destroy(gameObject);
    }
}