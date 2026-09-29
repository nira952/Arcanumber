using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerRoot root;
    private float currentInputDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(PlayerRoot playerRoot)
    {
        root = playerRoot;


        // 強制的に Dynamic にする
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    /// <summary>
    /// 移動入力の値を設定します。
    /// </summary>
    /// <param name="input">水平移動の入力値。通常は -1 ～ 1 の範囲。</param>
    public void SetMoveDirection(float input)
    {
        currentInputDirection = input;
    }

    /// <summary>
    /// Rigidbody の linearVelocity を設定して水平移動を更新する。
    /// </summary>
    public void UpdateMovement()
    {
        if (rb == null || root == null) return;

        // 速度を取得
        float moveSpeed = root.GetMoveSpeed();

        // linearVelocityを直接変更(X = 現在の移動方向 * 移動速度, Y = 現在の垂直速度)
        rb.linearVelocity = new Vector2(currentInputDirection * moveSpeed, rb.linearVelocity.y);

        // 位置の共有はNetWorkTransfromに依存
    }


    /// <summary>
    /// ジャンプの実行
    /// </summary>
    public void JumpActive()
    {
        if (rb == null || root == null) return;

        // ジャンプ力を取得
        float jumpForce = root.GetJumpForce();

        // linearVelocityを直接変更(X = 現在の水平速度, Y = ジャンプ力)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        // 落下はRigidbodyの重力に依存
    }

    /// <summary>
    /// 所有権がない場合など移動を強制的に停止する
    /// </summary>
    public void StopMovement()
    {
        if (rb == null || root == null) return;

        rb.linearVelocity = Vector2.zero; // 移動を停止
    }

    public Rigidbody2D GetRigidbody() => rb;
}