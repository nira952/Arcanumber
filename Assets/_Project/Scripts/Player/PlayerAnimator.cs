using R3;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// プレイヤーのアニメーション再生とパラメータ同期を担うクラス
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(OwnerNetworkAnimator))]
public class PlayerAnimator : NetworkBehaviour
{
    private Animator animator;
    private OwnerNetworkAnimator networkAnimator;

    [SerializeField] private GameObject youObject; // 自分のプレイヤーを示すオブジェクト（UIやエフェクト用）

    [SerializeField] private RuntimeAnimatorController[] animatorControllers = new RuntimeAnimatorController[4];

    private readonly NetworkVariable<int> syncedPlayerIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner // または Server（権限設計に合わせて変更）
    );

    // Animator パラメータ・ステートのハッシュ値（文字列検索のオーバーヘッド削減）
    private static readonly int IsDashHash = Animator.StringToHash("IsDash");
    private static readonly int MagicStateHash = Animator.StringToHash("Magic");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<OwnerNetworkAnimator>();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // ★ 2. NetworkVariable の値が同期・変更された時に全端末で Controller を適用
        syncedPlayerIndex.OnValueChanged += OnPlayerIndexChanged;

        // すでに設定されている場合（途中参加や初期化済みの場合）は即座に反映
        ApplyAnimatorController(syncedPlayerIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        syncedPlayerIndex.OnValueChanged -= OnPlayerIndexChanged;
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// PlayerRoot から初期化され、入力・状態ストリームを購読する
    /// </summary>
    public void Initialize(PlayerRoot root, PlayerInputController inputController,int playerIndex,IPlayerActionHandler actionHandler)
    {
        // playerIndex が配列の範囲外になっていないかチェック
        if (playerIndex < 0 || playerIndex >= animatorControllers.Length)
        {
            Debug.LogWarning($"PlayerIndex ({playerIndex}) が配列の範囲外です。(配列長: {animatorControllers.Length}) デフォルトの 0 を使用します。");
            playerIndex = Mathf.Clamp(playerIndex, 0, animatorControllers.Length - 1); // 範囲内に収める
        }
        if (IsOwner)
        {
            syncedPlayerIndex.Value = playerIndex;
            youObject.SetActive(true); // 自分のプレイヤーを示すオブジェクトを有効化
        }
        else
        {
            youObject.SetActive(false); // 他のプレイヤーでは無効化
        }

        animator.runtimeAnimatorController = animatorControllers[playerIndex];

        // ----------------------------------------------------
        // 1. 移動アニメーション（IsDash）の制御
        // ----------------------------------------------------
        // 入力値に応じて IsDash の bool を変更
        inputController.OnMoveAsObservable
            .Where(_ => actionHandler != null && actionHandler.CanProcessInput)
            .Subscribe(moveInput =>
            {
                // 移動入力がほぼ 0 でない場合、かつ移動可能状態（IsMove）なら True
                bool isDashing = Mathf.Abs(moveInput) > 0.01f && root.IsMove.Value;
                SetDash(isDashing);

                // ★ 入力値に応じて左右の向き（Scale）を切り替える
                if (root.IsMove.Value)
                {
                    Flip(moveInput);
                }
            }).AddTo(this);

        // 状態の変化（ノックバックや死亡等で IsMove が false になった場合）にも対応
        root.IsMove
            .Where(canMove => !canMove)
            .Subscribe(_ => SetDash(false))
            .AddTo(this);


        // ----------------------------------------------------
        // 2. スキル使用アニメーション（Magic）の再生
        // ----------------------------------------------------
        inputController.OnSkillUseAsObservable
            .Where(_ => actionHandler != null && actionHandler.CanProcessInput)
            .Subscribe(_ =>
            {
                PlayMagicAnimation();
            }).AddTo(this);
    }

    private void OnPlayerIndexChanged(int previousValue, int newValue)
    {
        ApplyAnimatorController(newValue);
    }

    /// <summary>
    /// 指定された Index の AnimatorController を適用する
    /// </summary>
    private void ApplyAnimatorController(int index)
    {
        if (animatorControllers != null && index >= 0 && index < animatorControllers.Length)
        {
            if (animatorControllers[index] != null)
            {
                animator.runtimeAnimatorController = animatorControllers[index];
            }
        }
    }

    /// <summary>
    /// Dashフラグの設定
    /// </summary>
    private void SetDash(bool isDash)
    {
        animator.SetBool(IsDashHash, isDash);
    }

    /// <summary>
    /// Magic アニメーションの再生処理
    /// </summary>
    private void PlayMagicAnimation()
    {
        // 1. ローカルの Animator で再生
        animator.Play(MagicStateHash, 0, 0f);

        // 2. ネットワーク経由で他クライアントへ Play(ステート直接再生) を同期させる
        if (IsSpawned)
        {
            networkAnimator.Animator.Play(MagicStateHash, 0, 0f);
        }
    }

    /// <summary>
    /// 移動入力の方向に応じて Visual の Scale.x を反転させる
    /// </summary>
    private void Flip(float moveInput)
    {
        // 入力がほぼ 0 の場合は直前の向きを維持
        if (Mathf.Abs(moveInput) <= 0.01f) return;

        Vector3 currentScale = transform.localScale;

        // 右移動 (moveInput > 0) なら Scale.x を正、左移動 (moveInput < 0) なら負にする
        // ※ 元のスプライトが「左向き」基準で作られている場合は、不等号を逆にしてください
        if (moveInput > 0f)
        {
            currentScale.x = -Mathf.Abs(currentScale.x);
        }
        else if (moveInput < 0f)
        {
            currentScale.x = Mathf.Abs(currentScale.x);
        }

        transform.localScale = currentScale;
    }
}