using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スキルのオブジェクトを管理するクラス
/// </summary>
public abstract class MagicObject : MonoBehaviour
{
    protected int haveCharaNo;    //スキルを出したキャラクターNo
    protected float dmg;  //ダメージ
    protected float keepTime;   //持続時間
    protected EffectAbility effect;
    protected Animator animator;    //アニメーター

    protected List<int> hitList = new List<int>();  //当たった人のリスト
    private Dictionary<int, float> stayTimers = new Dictionary<int, float>();   //持続ダメージ用のタイマー

    protected bool useAnimationEndEvent; // アニメ終了で消すかどうかのフラグ
    protected bool isMoving = false;    // 移動するかどうか
    protected float moveSpeed = 0f;     // 移動速度
    protected bool isKeepDmg = false;    // 持続ダメージかどうか

    public virtual void Initialize(int charaNo, Skill skill, Vector2 pos)
    {
        haveCharaNo = charaNo;
        dmg = PlayerUtility.GetFinalAtk(PlayerUtility.FindPlayerByNo(haveCharaNo), skill);
        keepTime = skill.GetKeepTime();
        useAnimationEndEvent = skill.GetEffectAnimation() != null;
        effect = skill.GetEffect();
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        //寿命管理
        if (keepTime > 0)
            Destroy(gameObject, keepTime);
        else
            useAnimationEndEvent = true;
    }

    /// <summary>
    /// スキルオブジェクトの移動や持続ダメージの設定
    /// </summary>
    protected void SetMovement(bool moving, float speed, bool isKeepDmg, bool animationEndDestroy)
    {
        isMoving = moving;
        moveSpeed = speed;
        this.isKeepDmg = isKeepDmg;
        useAnimationEndEvent = animationEndDestroy;
    }

    protected virtual void Update()
    {
        // 共通の寿命管理
        if (useAnimationEndEvent) CheckAnimationEnd();

        // 共通の移動管理
        if (isMoving)
            transform.position += transform.right * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// 位置の確定と回転の確定
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
    /// アニメーションの終了をチェックし、オブジェクトを消す
    /// </summary>
    private void CheckAnimationEnd()
    {
        if (animator == null) return;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(0))
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 当たった時の場合
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isKeepDmg) return;
        HandleHit(collision);
    }

    /// <summary>
    /// 持続ダメージの場合
    /// </summary>
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

    /// <summary>
    /// 当たった時の処理
    /// </summary>
    private void HandleHit(Collider2D collision)
    {
        //ターゲットかどうか
        NetworkPlayer targetPlayer = collision.GetComponent<NetworkPlayer>();
        if (targetPlayer != null)
        {
            //当たったかどうか
            int targetId = targetPlayer.GetNetworkId();
            if (hitList.Contains(targetId) || targetId == haveCharaNo) return;
            //当たった人のリストに追加
            hitList.Add(targetId);
            //ダメージ処理
            OnHit(targetPlayer);
            AtkHeal();
        }

        FragileMinion targetMinion = collision.GetComponent<FragileMinion>();
        if (targetMinion != null)
        {
            //自分の召喚物ではない場合
            if(targetMinion.GetwnerPlayerNo() != haveCharaNo)
                targetMinion.TakeDamage();
            return;
        }
    }

    protected abstract void OnHit(NetworkPlayer target);

    /// <summary>
    /// AtkHealを持っているときの処理
    /// </summary>
    private void AtkHeal()
    {
        //バフを持っていたら回復する
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(haveCharaNo);
        if (PlayerUtility.HaveEffect(player, EffectList.AtkHeal, true))
            PlayerUtility.FinalHeal(player, dmg * GameConfig.DEATH_BACK_VALUE);
    }
}
