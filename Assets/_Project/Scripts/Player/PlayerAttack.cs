using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

// プレイヤーの通常攻撃を管理するクラス
public class PlayerAttack : NetworkBehaviour
{
    [SerializeField] private PlayerRoot root;
    private PlayerUIManager playerUIManager;
    private float attackPower = 3.0f; // 攻撃力の倍率
    private float attackUpPower = 5.5f; // パワーアップ時の攻撃力の倍率
    private float attackWindupTime = 0.5f; // 攻撃の前振り時間

    private const float attackCoolTimeDuration = 0.5f; // 通常攻撃のクールタイムの時間

    private float currentCoolTime = 0f; // 現在のクールタイムの残り時間

    private bool isLocalMode = false; // ローカルモードかどうかのフラグ

    [SerializeField] private NormalSlash slashObject;

    private void Start()
    {
        if (slashObject == null)
        {
            Debug.LogError("Slash object is not assigned in the inspector.");
            return;
        }

        isLocalMode = PlayerDataManager.Instance.IsLocalMode;
    }

    public void Initialized(PlayerRoot root, PlayerUIManager playerUIManager, bool isPowerUp)
    {
        this.root = root;
        this.playerUIManager = playerUIManager;
        int playerIndex = root.PlayerIndex.Value;
        float damage = isPowerUp ? attackUpPower : attackPower;

        // 攻撃オブジェクトを初期化する
        slashObject.Initialize(playerIndex, damage, -1);
    }

    public void NormalAttackActive(Vector3 direction)
    {
        if (!IsActionReady())
        {
            Debug.Log("Attack is on cooldown.");
            return;
        }

        StartActionCoolTime();

        if (isLocalMode)
        {
            ExecuteAttack(direction, this.GetCancellationTokenOnDestroy()).Forget();

            return;
        }

        if (IsOwner)
        {
            // ① 攻撃ボタンを押した本人は、通信ラグを感じさせないために即座に実行する
            ExecuteAttack(direction, this.GetCancellationTokenOnDestroy()).Forget();

            // ② サーバーへ攻撃したことを通知する
            AttackServerRpc(direction);
        }
    }

    [ServerRpc]
    private void AttackServerRpc(Vector3 direction)
    {
        // サーバーが自分自身（ホスト）の攻撃でない場合のみ実行する（ホストは①ですでに実行済みのため）
        if (!IsOwner)
        {
            ExecuteAttack(direction, this.GetCancellationTokenOnDestroy()).Forget();
        }

        // ③ 攻撃者以外の全クライアントに攻撃を通知する
        AttackClientRpc(direction);
    }

    [ClientRpc]
    private void AttackClientRpc(Vector3 direction)
    {
        // 攻撃者本人（①で実行済）とサーバー（上で実行済）は二重実行になるためスキップ
        if (IsOwner || IsServer) return;

        ExecuteAttack(direction, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid ExecuteAttack(Vector3 direction, CancellationToken token)
    {

        // 向きを変更する（全プレイヤーの画面で同期して反映される）
        transform.localScale = direction;

        // 【注意】root.CanMove が NetworkVariable の場合はサーバーしか変更できません。
        // もし UniRx の ReactiveProperty などのローカル変数であれば、if文を外して全員が false になるようにしてください。
        //if (IsServer || isLocalMode)
        //{
        //    root.CanMove.Value = false; // 攻撃中は移動を禁止する
        //}

        await UniTask.Delay((int)(attackWindupTime * 1000), cancellationToken: token);

        // 攻撃アニメーション・当たり判定を全員の画面で実行する
        slashObject.ActiveAttack();

        // エフェクトも別RPCではなく、ここに統合して全員の画面で一括再生する
        slashObject.ActiveAniation("RedSlash");

        await UniTask.Delay(300, cancellationToken: token); // 待機

        // 攻撃終了処理を全員の画面で実行する
        slashObject.EndAttack();

        //if (IsServer || isLocalMode)
        //{
        //    root.CanMove.Value = true; // 攻撃終了後に移動を許可する
        //}
    }

    public void ResetList()
    {
        slashObject.ResetList();
    }

    /// <summary>
    /// クールタイムの管理
    /// </summary>
    public bool IsActionReady() => currentCoolTime <= 0f;

    /// <summary>
    /// クールタイムの開始
    /// </summary>
    public void StartActionCoolTime()
    {
        currentCoolTime = CoolTimeValue(attackCoolTimeDuration);
    }


    private float CoolTimeValue(float duration)
    {
        // エフェクトの値を取得し、クールタイム短縮を計算
        float effectMultiplier = root.GetEffectValue(EffectList.CoolTimeReduction);
        float reductionRate = 1.0f - (effectMultiplier - 1.0f);

        // クールタイム短縮を適用し、下限を確保して返す
        return Mathf.Max(0.1f, duration * reductionRate);
    }

    /// <summary>
    /// クールタイムの更新
    /// </summary>
    public void UpdateAttackCoolTime()
    {
        if (currentCoolTime > 0f)
        {
            currentCoolTime -= Time.deltaTime;
            //マイナス防止
            if (currentCoolTime < 0f) currentCoolTime = 0f;
        }

        if (playerUIManager != null)
        {
            playerUIManager.UpdateAttackCoolTimeUI(currentCoolTime, attackCoolTimeDuration);
        }
    }

}