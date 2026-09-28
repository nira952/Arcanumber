using R3;
using UnityEngine;

public class HpBullet : EnvironmentObject
{
    private float speed = 4f;
    private float damage = 0.5f;
    private float value = 0.3f;

    //スキルとして登録しないのであれば、独自の初期化メソッド名でOK
    public void InitializeBullet(int charaNo, Vector2 pos)
    {
        //親の初期化を呼び出す
        base.Initialize(charaNo, pos, damage, speed, false, 0);
    }

    protected override void OnHit(PlayerRoot target)
    {
        base.OnHit(target);
        onDestroyed.OnNext(Unit.Default);
    }
}
