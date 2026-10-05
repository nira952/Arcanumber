using UnityEngine;

public class Fire : SkillObject
{
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private bool hasHitGround = false;

    // スポーン時の初期スケールを保持しておく変数
    private Vector3 initialLossyScale;

    protected override void CommonInitialize(int attackerIndex, float damage)
    {
        base.CommonInitialize(attackerIndex, damage);
        gameObject.transform.rotation = Quaternion.identity;

        // 【重要】もしキャラクターの子として生成されていて、親の反転影響を受けたくない場合は
        //ここで親を解除してワールド直下に配置する（必要に応じて有効化してください）
        // transform.parent = null;

        // コンポーネントを取得
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
        if (hasHitGround)
        {
            base.Update();
            return;
        }

        // --- 地面に着くまでの処理 ---
        // 常に真下（Vector2.down）へレイキャスト
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 10f, LayerMask.GetMask("Ground", "Wall"));

        if (hit.collider != null)
        {
            float offset = 0f;
            if (boxCollider != null)
            {
                float scaleY = Mathf.Abs(transform.localScale.y);
                offset = ((boxCollider.size.y * 0.5f) - boxCollider.offset.y) * scaleY;
            }

            transform.position = new Vector3(hit.point.x, hit.point.y + offset, transform.position.z);

            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (boxCollider != null) boxCollider.enabled = true;

            isKeepDmg = true;
            hasHitGround = true;
        }
        else
        {
            // 常にまっすぐ下に落ちる
            transform.position += Vector3.down * 10f * Time.deltaTime;
        }
    }
}