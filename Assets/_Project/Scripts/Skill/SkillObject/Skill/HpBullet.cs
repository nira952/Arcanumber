using R3;
using UnityEngine;

public class HpBullet : EnvironmentObject, IRpcObjectInterface
{
    private float speed = 4f;
    private float damage = 0.5f;



    public void RpcInitialize(int playerIndex)
    {
        Vector3 spawnPos = PlayerUtility.GetPlayerByIndex(playerIndex).transform.position; //+ Vector3.up * 1.5f;

        base.Initialize(playerIndex, spawnPos, damage, speed, false, 0);

    }

    protected override void OnHit(PlayerRoot target)
    {
        base.OnHit(target);
        onDestroyed.OnNext(Unit.Default);
    }
}
