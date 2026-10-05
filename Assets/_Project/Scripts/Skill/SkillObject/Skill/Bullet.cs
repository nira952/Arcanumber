using UnityEngine;
using R3;
public class Bullet : EnvironmentObject, IRpcObjectInterface
{
    private float speed = 10f;
    private float damage = 0.5f;

    public float length = 30f;  //範囲
    Vector2 pos = new Vector2(0, 7.5f);


    public void RpcInitialize(int playerIndex)
    {
        //範囲内でランダムなX
        float randomX = Random.Range(-length / 2f, length / 2f);
        //生成位置（高さは固定7.5f）
        Vector3 spawnPos = new Vector3(pos.x + randomX, pos.y, 0);

        transform.rotation = Quaternion.Euler(0, 0, -90f);
        base.Initialize(playerIndex, spawnPos, damage, speed, false, 0);
    }

    public void BulletInitialize(int playerIndex,Vector2 spawnPos)
    {
        base.Initialize(playerIndex, spawnPos, damage, speed, false, 0);
    }


    protected override void OnHit(PlayerRoot target)
    {
        base.OnHit(target);
        onDestroyed.OnNext(Unit.Default);
    }
}