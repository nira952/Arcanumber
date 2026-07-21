using UnityEngine;

/// <summary>
/// スキル経由ではないオブジェクト（トラップや単発エフェクトなど）
/// </summary>
public class EnvironmentObject : MagicObject
{
    private float moveSpeed = 0f;
    private bool isMoving = false;

    public void Initialize(int charaNo, Vector2 pos, float damage, float speed, bool penetrate, int reflect)
    {
        CommonInitialize(charaNo, damage);

        transform.position = pos;
        moveSpeed = speed;
        isMoving = (moveSpeed > 0);
        isPenetrate = penetrate;
        reflectCount = reflect;
        useAnimationEndEvent = (animator != null);
    }

    protected override void Update()
    {
        base.Update();

        if (isMoving)
            transform.position += transform.right * moveSpeed * Time.deltaTime;
    }

    protected override void OnHit(NetworkPlayer target)
    {
    }
}