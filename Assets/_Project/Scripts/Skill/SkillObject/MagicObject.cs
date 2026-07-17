using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スキルのオブジェクトを管理するクラス
/// </summary>
public abstract class MagicObject : MonoBehaviour
{
    protected int haveCharaNo;    //スキルを出したキャラクターNo
    protected float dmg;          //ダメージ
    protected float keepTime;     //持続時間
    protected EffectAbility effect;
    protected Animator animator;  //アニメーター

    protected List<int> hitList = new List<int>();               //当たった人のリスト
    private Dictionary<int, float> stayTimers = new Dictionary<int, float>(); //持続ダメージ用タイマー

    protected bool useAnimationEndEvent;           //アニメ終了で消すかどうかのフラグ
    protected bool isHitAndWaitingDestroy = false; //命中済み・消滅待ちフラグ
    protected bool isMoving = false;               //移動するかどうか
    protected float moveSpeed = 0f;                //移動速度
    protected bool isKeepDmg = false;              //持続ダメージかどうか

    protected bool isPenetrate = false; //貫通するか
    protected int reflectCount = 0;     //残りの反射可能回数

    protected void CommonInitialize(int charaNo, Vector2 pos)
    {
        haveCharaNo = charaNo;
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        SetupPositionAndRotation(pos);
    }

    public virtual void Initialize(int charaNo, Skill skill, Vector2 pos)
    {
        CommonInitialize(charaNo, pos);

        dmg = PlayerUtility.GetFinalAtk(PlayerUtility.FindPlayerByNo(haveCharaNo), skill);
        keepTime = skill.GetKeepTime();
        effect = skill.GetEffect();
        moveSpeed = skill.GetMoveSpeed();
        isMoving = (moveSpeed > 0);
        isPenetrate = skill.GetIsPenetrate();
        reflectCount = skill.GetReflectCount();
        useAnimationEndEvent = skill.GetEffectAnimation() != null;

        if (keepTime > 0) Destroy(gameObject, keepTime);
    }

    public virtual void Initialize(int charaNo, Vector2 pos, float damage, float speed, bool penetrate, int reflect)
    {
        CommonInitialize(charaNo, pos);

        this.dmg = damage;
        this.moveSpeed = speed;
        this.isMoving = (moveSpeed > 0);
        this.isPenetrate = penetrate;
        this.reflectCount = reflect;
        // スキル以外のものは、必要なら適宜フラグを立てる
        this.useAnimationEndEvent = (animator != null);
    }

    protected virtual void Update()
    {
        //アニメーション終了待ち（持続ダメージ中は無効）
        if (useAnimationEndEvent && !isKeepDmg) CheckAnimationEnd();

        //移動処理
        if (isMoving)
            transform.position += transform.right * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// 位置と回転の確定
    /// </summary>
    public void SetupPositionAndRotation(Vector2 pos)
    {
        if (!isMoving)
            transform.position = pos;
        else
        {
            Vector2 direction = (pos - (Vector2)transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// アニメーションの終了をチェックして消滅させる
    /// </summary>
    private void CheckAnimationEnd()
    {
        if (animator == null) return;

        //貫通・反射がまだ残っている場合はアニメ終了では消さない
        if (isPenetrate || reflectCount > 0) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(0))
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //持続ダメージ中、または既に命中済みの場合は無視
        if (isKeepDmg || isHitAndWaitingDestroy) return;
        HandleHit(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isKeepDmg) return;

        NetworkPlayer target = collision.GetComponent<NetworkPlayer>();
        if (target == null) return;

        int id = target.GetNetworkId();
        if (!stayTimers.ContainsKey(id)) stayTimers[id] = 0f;
        stayTimers[id] += Time.deltaTime;

        //0.5秒ごとにダメージ
        if (stayTimers[id] >= 0.5f)
        {
            stayTimers[id] = 0f;
            OnHit(target);
            AtkHeal();
        }
    }

    private void HandleHit(Collider2D collision)
    {
        //プレイヤーへの処理
        NetworkPlayer targetPlayer = collision.GetComponent<NetworkPlayer>();
        if (targetPlayer != null)
        {
            int targetId = targetPlayer.GetNetworkId();
            if (hitList.Contains(targetId) || targetId == haveCharaNo) return;

            hitList.Add(targetId);
            OnHit(targetPlayer);
            AtkHeal();

            //貫通：消滅させない
            if (isPenetrate) return;

            //反射：回数を減らして向きを変える
            if (reflectCount > 0)
            {
                reflectCount--;
                transform.right = -transform.right;
                return;
            }

            //通常：アニメ演出があれば終了を待ち、なければ即消滅
            if (useAnimationEndEvent)
                isHitAndWaitingDestroy = true;
            else
                Destroy(gameObject);
            return;
        }

        //ミニオンへの処理
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