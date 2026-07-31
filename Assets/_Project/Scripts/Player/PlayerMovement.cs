using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerRoot root;
    private float currentMoveInput;

    public void Initialize(PlayerRoot playerRoot)
    {
        root = playerRoot;
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetMoveInput(float input)
    {
        currentMoveInput = input;
    }

    public void UpdateMovement()
    {
        if (rb == null || root == null) return;

        // ★ 1. Rigidbody2D が Kinematic の場合は速度直接代入が効かないためガード
        if (rb.isKinematic) return;

        // ★ 2. Transform 座標を直接移動させるのではなく、物理速度を設定
        rb.linearVelocity = new Vector2(currentMoveInput * root.GetMoveSpeed(), rb.linearVelocity.y);
    }

    public void ExecuteJump()
    {
        if (rb == null || root == null) return;
        if (rb.isKinematic) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, root.GetJumpForce());
    }

    public Rigidbody2D GetRigidbody() => rb;
}