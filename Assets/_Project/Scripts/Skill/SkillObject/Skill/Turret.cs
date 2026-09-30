using R3;
using UnityEngine;

public class Turret : MonoBehaviour, IRpcObjectInterface
{
    private bool isOwner = false;

    private int attackerIndex;
    float createSpeed = 1f;
    float time = 0;

    private Subject<Unit> onDestroyed = new Subject<Unit>();
    public Observable<Unit> OnDestroyed => onDestroyed;

    public void RpcInitialize(int playerIndex)
    {
        attackerIndex = playerIndex;

        TurretDirection();

        isOwner = true;
    }

    void Update()
    {
        if (!isOwner) return;

        TimeCount();
    }

    /// <summary>
    /// タレットの向きを決めるメソッド
    /// </summary>
    void TurretDirection()
    {
        //０より大きいなら左、小さいなら右
        if (transform.position.x >= 0)
            transform.rotation = Quaternion.Euler(0, 180, 0);
        else
            transform.rotation = Quaternion.Euler(0, 0, 0);
    }
    
    /// <summary>
    /// カウントダウン
    /// </summary>
    void TimeCount()
    {
        time += Time.deltaTime;
        if(time > createSpeed)
        {
            InstanceBullet();
            time = 0;
        }
    }

    /// <summary>
    /// バレットの生成
    /// </summary>
    void InstanceBullet()
    {
        //弾を生成
        SkillManager.Instance.SpawnBulletObject(attackerIndex, transform.position, transform.right);
    }


}
