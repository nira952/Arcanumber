using UnityEngine;

/// <summary>
/// スキルオブジェクトが消える用のクラス
/// </summary>
public class Wall : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag != "Ground")
            Destroy(collision.gameObject);
    }
}
