using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// すべての魔法・スキル・オブジェクトの共通基底クラス
/// </summary>
public abstract class MagicObject : MonoBehaviour
{
    protected int haveCharaNo;    //出したキャラクターNo
    protected float dmg;          //ダメージ
    protected float keepTime;     //持続時間
    protected Animator animator;  //アニメーター
    protected EffectAbility effect;    //付与するエフェクト

    protected List<int> hitList = new List<int>();
    private Dictionary<int, float> stayTimers = new Dictionary<int, float>();

    protected bool useAnimationEndEvent;
    protected bool isHitAndWaitingDestroy = false;
    protected bool isKeepDmg = false;
    protected bool isPenetrate = false;
    protected int reflectCount = 0;

    protected virtual void CommonInitialize(int charaNo, float damage)
    {
        haveCharaNo = charaNo;
        dmg = damage;
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    protected virtual void Update()
    {
        if (useAnimationEndEvent && !isKeepDmg) CheckAnimationEnd();
    }

    private void CheckAnimationEnd()
    {
        if (animator == null) return;
        if (isPenetrate || reflectCount > 0) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(0))
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isKeepDmg || isHitAndWaitingDestroy) return;
        HandleHit(collision);
    }

    private void HandleHit(Collider2D collision)
    {
        NetworkPlayer targetPlayer = collision.GetComponent<NetworkPlayer>();
        if (targetPlayer != null)
        {
            int targetId = targetPlayer.GetNetworkId();
            if (hitList.Contains(targetId) || targetId == haveCharaNo) return;

            hitList.Add(targetId);
            OnHit(targetPlayer);
            AtkHeal();

            if (isPenetrate) return;
            if (reflectCount > 0)
            {
                HandleReflect(collision);
                return;
            }

            if (useAnimationEndEvent) isHitAndWaitingDestroy = true;
            else Destroy(gameObject);
            return;
        }

        FragileMinion targetMinion = collision.GetComponent<FragileMinion>();
        if (targetMinion != null && targetMinion.GetwnerPlayerNo() != haveCharaNo)
        {
            targetMinion.TakeDamage();
            if (!isPenetrate) Destroy(gameObject);
        }

        if(collision.tag == "Wall" ||  collision.tag == "Ground")
        {
            if (reflectCount > 0)
            {
                HandleReflect(collision);
                return;
            }
        }
    }

    /// <summary>
    /// 反射処理を行う
    /// </summary>
    private void HandleReflect(Collider2D collision)
    {
        if (reflectCount <= 0) return;

        reflectCount--;

        //自身の位置から相手のコライダー上で一番近い点を取得
        Vector2 hitPoint = collision.ClosestPoint(transform.position);

        //衝突点から自分の中心へ向かうベクトル
        Vector2 normal = ((Vector2)transform.position - hitPoint).normalized;

        //もし近すぎてnormalがゼロベクトルになってしまった場合
        if (normal == Vector2.zero)
            normal = -transform.right;

        //反射計算を行う
        Vector2 reflectDir = Vector2.Reflect(transform.right, normal);
        transform.right = reflectDir;
    }

    protected abstract void OnHit(NetworkPlayer target);

    protected void AtkHeal()
    {
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(haveCharaNo);
        if (PlayerUtility.HaveEffect(player, EffectList.AtkHeal, true))
            PlayerUtility.FinalHeal(player, dmg * GameConfig.DEATH_BACK_VALUE);
    }
}