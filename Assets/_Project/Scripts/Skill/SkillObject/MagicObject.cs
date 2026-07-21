using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// すべての魔法・スキル・オブジェクトの共通基底クラス
/// </summary>
public abstract class MagicObject : MonoBehaviour
{
    protected int haveCharaNo;    // 出したキャラクターNo
    protected float dmg;          // ダメージ
    protected float keepTime;     // 持続時間
    protected Animator animator;  // アニメーター
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
                reflectCount--;
                transform.right = -transform.right;
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
    }

    protected abstract void OnHit(NetworkPlayer target);

    protected void AtkHeal()
    {
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(haveCharaNo);
        if (PlayerUtility.HaveEffect(player, EffectList.AtkHeal, true))
            PlayerUtility.FinalHeal(player, dmg * GameConfig.DEATH_BACK_VALUE);
    }
}