using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 召喚物のクラス
/// </summary>
public class FragileMinion : MonoBehaviour
{
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

    public void Initialize(int playerNo)
    {
        ownerPlayerNo = playerNo;
    }

    void Update()
    {
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
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// ターゲットを探す
    /// </summary>
    private Transform FindTarget()
    {
        List<NetworkPlayer> players = PlayerUtility.GetOtherPlayers(
            PlayerUtility.FindPlayerByNo(ownerPlayerNo));
        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (NetworkPlayer p in players)
        {
            //自分の持ち主ではないプレイヤーを探す
            if (p.GetNetworkId() != ownerPlayerNo)
            {
                float dist = Vector3.Distance(transform.position, p.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = p.transform;
                }
            }
        }
        return closest;
    }

    public void TakeDamage()
    {
        hitCount++;

        if (hitCount >= MaxHits)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 敵に接触したときの攻撃処理
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryAttack(collision);
    }

    /// <summary>
    /// 触れ続けている間の攻撃処理
    /// </summary>
    private void OnTriggerStay2D(Collider2D collision)
    {
        TryAttack(collision);
    }

    /// <summary>
    /// 攻撃処理
    /// </summary>
    private void TryAttack(Collider2D collision)
    {
        //クールタイムチェック
        if (Time.time - lastAttackTime < attackCooldown) return;

        NetworkPlayer targetPlayer = collision.GetComponent<NetworkPlayer>();

        //持ち主以外のプレイヤーに当たったらダメージ
        if (targetPlayer != null && targetPlayer.GetNetworkId() != ownerPlayerNo)
        {
            targetPlayer.TakeDamage(attackDmg);

            //攻撃時刻を更新
            lastAttackTime = Time.time;
            if (animator != null)
                animator.SetTrigger("Itching_Akita");
        }
    }
    public int GetwnerPlayerNo() => ownerPlayerNo;
}
