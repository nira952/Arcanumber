using UnityEngine;

/// <summary>
/// プレイヤーの物理的な移動とジャンプ処理を担うクラス
/// </summary>
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
        Debug.Log($"[PlayerMovement] UpdateMovement called with input: {currentMoveInput}");

        if (rb != null && root != null)
        {
            rb.linearVelocity = new Vector2(currentMoveInput * root.GetMoveSpeed(), rb.linearVelocity.y);
        }
    }

    public void ExecuteJump()
    {
        if (rb != null && root != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, root.GetJumpForce());
        }
    }

    public Rigidbody2D GetRigidbody() => rb;
}