using UnityEngine;

/// <summary>
/// RayキャストやOverlap判定など、空間検知を担うクラス
/// </summary>
public class PlayerRayInput : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private Color groundedColor = Color.green;
    [SerializeField] private Color airColor = Color.red;

    public bool IsGrounded(Rigidbody2D rb)
    {
        if (groundCheck == null) return false;

        // 上昇中（y速度が一定以上）は接地判定を無効にする
        bool isMovingUp = rb != null && rb.linearVelocity.y > 0.01f;

        // 足元の小さな円の範囲にgroundLayerがあるか判定
        bool isGrounded = !isMovingUp && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // --- デバッグ用の表示処理 ---
        if (showDebugRay)
        {
            DrawDebugLines(groundCheck.position, isGrounded);
        }

        return isGrounded;
    }

    /// <summary>
    /// 接地チェック位置に十字と簡易的な円のラインを描画する
    /// </summary>
    private void DrawDebugLines(Vector3 centerPosition, bool isGrounded)
    {
        // 接地状態に合わせて色を変更（接地: 緑, 滞空: 赤）
        Color lineColor = isGrounded ? groundedColor : airColor;

        // 1. 中心位置を示す十字マーク
        Debug.DrawLine(centerPosition + Vector3.left * groundCheckRadius, centerPosition + Vector3.right * groundCheckRadius, lineColor);
        Debug.DrawLine(centerPosition + Vector3.down * groundCheckRadius, centerPosition + Vector3.up * groundCheckRadius, lineColor);

        // 2. 判定範囲（半径）の目安となる上下左右のRay
        float halfRadius = groundCheckRadius * 0.707f; // 45度方向の計算用
        Debug.DrawLine(centerPosition, centerPosition + new Vector3(halfRadius, halfRadius, 0), lineColor);
        Debug.DrawLine(centerPosition, centerPosition + new Vector3(-halfRadius, halfRadius, 0), lineColor);
        Debug.DrawLine(centerPosition, centerPosition + new Vector3(halfRadius, -halfRadius, 0), lineColor);
        Debug.DrawLine(centerPosition, centerPosition + new Vector3(-halfRadius, -halfRadius, 0), lineColor);
    }

    /// <summary>
    /// エディタ上でオブジェクトを選択した際に、判定用の円（WireSphere）を常に可視化する
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}