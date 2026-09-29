using R3;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// すべての魔法・スキル・オブジェクトの共通基底クラス
/// </summary>
public abstract class MagicObject : MonoBehaviour
{
    protected int haveAttackerIndex = -1; // -1で未初期化を表す
    protected float dmg;          //ダメージ
    protected float keepTime;     //持続時間
    protected Animator animator;  //アニメーター
    protected EffectAbility effect;    //付与するエフェクト
    protected SeName se;          //再生するSE

    private BoxCollider2D areaCollider; //持続ダメージ用のエリアコライダー
    protected List<int> hitList = new List<int>();
    private Dictionary<int, float> stayTimers = new Dictionary<int, float>();

    protected bool useAnimationEndEvent;
    protected bool isHitAndWaitingDestroy = false;
    protected bool isKeepDmg = false;
    protected bool isPenetrate = false;
    protected int reflectCount = 0;

    // 破棄されたことを通知するためのSubject
    protected Subject<Unit> onDestroyed = new Subject<Unit>();
    public Observable<Unit> OnDestroyed => onDestroyed;

    /// <summary>
    /// NetworkBehaviour 以外からでもサーバー上かどうかを判定するヘルパー
    /// </summary>
    protected bool IsServer()
    {
        // ネットワークが非アクティブ（オフライン/テスト時）は true、マルチ時は IsServer を参照
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsLocalMode) return true;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.isActiveAndEnabled) return true;
        return NetworkManager.Singleton.IsServer;
    }

    protected virtual void CommonInitialize(int attackerIndex, float damage)
    {
        haveAttackerIndex = attackerIndex;
        dmg = damage;
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        NetWorkAudioManager.Instance.PlayGlobal(se);
    }

    protected virtual void Awake()
    {
        //範囲判定用コライダーをあらかじめ取得しておく
        areaCollider = GetComponent<BoxCollider2D>() ?? GetComponentInChildren<BoxCollider2D>();
    }

    protected virtual void Update()
    {
        if (useAnimationEndEvent && !isKeepDmg) CheckAnimationEnd();
        // 持続ダメージの処理はサーバー上でのみ行う
        if (IsServer() && isKeepDmg) ProcessAreaDamage();
    }

    private void CheckAnimationEnd()
    {
        if (animator == null) return;
        if (isPenetrate || reflectCount > 0) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(0))
            onDestroyed.OnNext(Unit.Default);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isKeepDmg || isHitAndWaitingDestroy) return;
        HandleHit(collision);
    }

    private void ProcessAreaDamage()
    {
        if (areaCollider == null) return;

        //自分のコライダーの範囲内に重なっているすべてのColliderを検出する
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true; //トリガーコライダーも含める
        List<Collider2D> results = new List<Collider2D>();
        Physics2D.OverlapCollider(areaCollider, filter, results);

        //現在エリア内にいるプレイヤーのインデックスを一時的に記録するセット
        HashSet<int> currentFramePlayers = new HashSet<int>();

        foreach (var col in results)
        {
            PlayerRoot targetPlayer = col.GetComponentInParent<PlayerRoot>();
            if (targetPlayer != null)
            {
                int targetIndex = targetPlayer.PlayerIndex.Value;
                //自分自身や未初期化は除外
                if (targetIndex == -1 || targetIndex == haveAttackerIndex) continue;

                currentFramePlayers.Add(targetIndex);

                //ターゲットごとのタイマー初期化
                if (!stayTimers.ContainsKey(targetIndex))
                    stayTimers[targetIndex] = 0f;

                stayTimers[targetIndex] += Time.deltaTime;

                //一定間隔を超えたら持続ダメージを適用
                if (stayTimers[targetIndex] >= GameConfig.DAMAGE_INTERVAL)
                {
                    stayTimers[targetIndex] = 0f; //タイマーリセット

                    OnHit(targetPlayer);
                    AtkHeal();
                }
            }
        }

        //エリア外に出たプレイヤーのタイマーを削除
        List<int> keysToRemove = new List<int>();
        foreach (var kvp in stayTimers)
        {
            if (!currentFramePlayers.Contains(kvp.Key))
                keysToRemove.Add(kvp.Key);
        }
        foreach (var key in keysToRemove)
            stayTimers.Remove(key);
    }
    private void HandleHit(Collider2D collision)
    {
        if (!IsServer()) return;  // サーバー上でのみ処理

        // 1. プレイヤー判定（子オブジェクトのコライダーも考慮）
        PlayerRoot targetPlayer = collision.GetComponentInParent<PlayerRoot>();
        if (targetPlayer != null)
        {
            Debug.Log($"MagicObject hit Player: {targetPlayer.PlayerIndex.Value} by Attacker: {haveAttackerIndex}");

            // 2. ターゲットのインデックスを取得
            int targetIndex = targetPlayer.PlayerIndex.Value;

            // 自分自身への当たりの除外 & 重複ヒット防止
            if (targetIndex == -1 || hitList.Contains(targetIndex) || targetIndex == haveAttackerIndex) return;

            hitList.Add(targetIndex);
            OnHit(targetPlayer);
            AtkHeal();

            // 貫通しない場合は破棄
            if (isPenetrate) return;
            if (reflectCount > 0)
            {
                HandleReflect(collision);
                return;
            }

            if (useAnimationEndEvent) isHitAndWaitingDestroy = true;
            else onDestroyed.OnNext(Unit.Default);  // 破棄通知
            return;
        }

        //ミニオン判定
        FragileMinion targetMinion = collision.GetComponentInParent<FragileMinion>();
        if (targetMinion != null && targetMinion.GetwnerPlayerNo() != haveAttackerIndex)
        {
            targetMinion.ApplyDamage();
            if (!isPenetrate) onDestroyed.OnNext(Unit.Default);
            return;
        }

        //地形・壁判定（CompareTagで軽量化 & 反射切れてからの破棄）
        if (collision.CompareTag("Wall") || collision.CompareTag("Ground"))
        {
            if (reflectCount > 0)
            {
                HandleReflect(collision);
            }
            else if (!isPenetrate)
            {
                if (useAnimationEndEvent) isHitAndWaitingDestroy = true;
                else onDestroyed.OnNext(Unit.Default);
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

    protected virtual void OnHit(PlayerRoot target)
    {
        //ダメージを与える
        PlayerUtility.FinalDamage(target.PlayerIndex.Value, haveAttackerIndex, dmg);
        // エフェクトがある場合、付与する
        if (effect != null)
            target.AddEffect(effect.Clone());
    }


    protected void AtkHeal()
    {
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(haveAttackerIndex);
        // 攻撃者がAtkHeal効果を持っている場合、ダメージの一部を回復する
        if (player != null && player.HaveEffect(EffectList.AtkHeal, true))
            player.ApplyHeal(dmg * player.GetEffectValue(EffectList.AtkHeal));
    }
}