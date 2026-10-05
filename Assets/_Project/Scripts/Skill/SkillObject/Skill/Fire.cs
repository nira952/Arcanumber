using UnityEngine;

public class Fire : SkillObject
{
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private bool hasHitGround = false;

    protected override void CommonInitialize(int attackerIndex, float damage)
    {
        base.CommonInitialize(attackerIndex, damage);

        // コンポーネントを取得（自分自身または子オブジェクトから）
        spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>() ?? GetComponentInChildren<BoxCollider2D>();

        // 最初はスプライトもコライダーも非表示・無効化しておく
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (boxCollider != null) boxCollider.enabled = false;

        // エリア魔法としての持続ダメージを有効にするフラグを立てる
        isKeepDmg = false;
    }

    protected override void Update()
    {
        // すでに地面に当たっている場合は、親クラスの通常のUpdateや持続ダメージ処理を行う
        if (hasHitGround)
        {
            base.Update();
            return;
        }

        // --- 地面に着くまでの処理 ---
        // 現在の位置から真下に向かってレイキャストを飛ばす
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 10f, LayerMask.GetMask("Ground", "Wall"));

        if (hit.collider != null)
        {
            float offset = 0f;
            if (boxCollider != null)
            {
                // scale.yがマイナス（反転）していても、Mathf.Absで絶対値（正の値）にすることで
                // 2P側でも計算結果が狂わず、常に正しい高さのオフセットを計算できるようにする
                float scaleY = Mathf.Abs(transform.localScale.y);
                offset = ((boxCollider.size.y * 0.5f) - boxCollider.offset.y) * scaleY;
            }

            // 地面に当たった位置（hit.point.y）にオフセットを足して、地面の上辺に下辺を合わせる
            transform.position = new Vector3(hit.point.x, hit.point.y + offset, transform.position.z);

            // 着弾したので、SpriteとBoxColliderを有効化する
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (boxCollider != null) boxCollider.enabled = true;

            // 持続ダメージを有効にする
            isKeepDmg = true;
            hasHitGround = true;
        }
        else
        {
            // まだ地面に当たっていない間は下に落ちていく
            transform.position += Vector3.down * 10f * Time.deltaTime;
        }
    }
}