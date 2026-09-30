using R3;
using UnityEngine;

/// <summary>
/// 召喚物のクラス
/// </summary>
public class FragileMinion : MonoBehaviour, IRpcObjectInterface
{
    private bool isOwner = false;  //自分の召喚物かどうか

    private int ownerPlayerNo;   //出したプレイヤー番号
    private const int MaxHits = 2;  //体力
    private int hitCount = 0;   //攻撃された回数
    private float lifeTime = 30f;   //ライフ時間
    private float attackDmg = 0.5f; //攻撃力

    private float attackCooldown = 1.0f; // 1秒ごとに攻撃する場合
    private float lastAttackTime = 0f;  //経過時間

    private float moveSpeed = 3.0f; //移動速度
    private Transform target;   //ターゲット位置

    [SerializeField] private Animator animator;          //アニメーター
    [SerializeField] private SpriteRenderer spriteRenderer; //反転用


    private Subject<Unit> destroyedSubject = new Subject<Unit>();
    public Observable<Unit> OnDestroyed => destroyedSubject;

    public void RpcInitialize(int playerIndex)
    {
        Debug.Log($"FragileMinion: RpcInitialize called with playerIndex {playerIndex}");

        ownerPlayerNo = playerIndex;

        isOwner = true;
    }

    void Update()
    {
        if (!isOwner) return;

        CountDown();

        target = FindTarget();

        //移動する
        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            if (spriteRenderer != null)
            {
                if (direction.x > 0)
                    spriteRenderer.flipX = false; //右向き
                else if (direction.x < 0)
                    spriteRenderer.flipX = true;  //左向き
            }
        }

    }

    /// <summary>
    /// カウントダウン計算
    /// </summary>
    void CountDown()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0)
        {
            destroyedSubject.OnNext(Unit.Default);
            return;
        }
    }

    /// <summary>
    /// ターゲットを探す
    /// </summary>
    private Transform FindTarget()
    {
        // 0～3のランダムなプレイヤー番号を生成
        int randomPlayerNo = Random.Range(0, 4);

        // プレイヤー番号が自分の番号と一致する場合は再度生成
        while (randomPlayerNo == ownerPlayerNo)
        {
            randomPlayerNo = Random.Range(0, 4);
        }

        // ターゲットのプレイヤーを探す
        PlayerRoot target = PlayerUtility.GetPlayerByIndex(randomPlayerNo);

        if (target == null)
        {
            Debug.LogWarning($"FragileMinion: No player found with index {randomPlayerNo}");
            return null;
        }


        return target.transform;
    }

    public void ApplyDamage()
    {
        hitCount++;

        if (hitCount >= MaxHits)
        {
            destroyedSubject.OnNext(Unit.Default);
        }
    }

    /// <summary>
    /// 敵に接触したときの攻撃処理
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isOwner) return;

        TryAttack(collision);
    }

    /// <summary>
    /// 触れ続けている間の攻撃処理
    /// </summary>
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isOwner) return;

        TryAttack(collision);
    }

    /// <summary>
    /// 攻撃処理
    /// </summary>
    private void TryAttack(Collider2D collision)
    {
        //クールタイムチェック
        if (Time.time - lastAttackTime < attackCooldown) return;

        PlayerRoot targetPlayer = collision.GetComponent<PlayerRoot>();

        //持ち主以外のプレイヤーに当たったらダメージ
        if (targetPlayer != null && targetPlayer.PlayerIndex.Value != ownerPlayerNo)
        {
            PlayerUtility.FinalDamage(targetPlayer.PlayerIndex.Value, ownerPlayerNo, attackDmg);

            //攻撃時刻を更新
            lastAttackTime = Time.time;
            if (animator != null)
                animator.SetTrigger("Itching_Akita");
        }
    }
    public int GetwnerPlayerNo() => ownerPlayerNo;
}
