using UnityEngine;

/// <summary>
/// プレイヤーがスキルやAimCursorを使って発動するオブジェクト
/// </summary>
public class SkillObject : MagicObject
{
    private bool isMoving = false;
    private float moveSpeed = 0f;

    public void Initialize(int charaNo, Skill skill, Vector2 targetPos)
    {
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(charaNo);
        float finalDmg = PlayerUtility.GetFinalAtk(player, skill);

        //発射元の決定
        Vector2 spawnPos = player.transform.position;

        CommonInitialize(charaNo, finalDmg);

        keepTime = skill.GetKeepTime();
        moveSpeed = skill.GetMoveSpeed();
        isMoving = (moveSpeed > 0);
        isPenetrate = skill.GetIsPenetrate();
        reflectCount = skill.GetReflectCount();
        useAnimationEndEvent = skill.GetEffectAnimation() != null;
        effect = skill.GetEffect();

        // 移動するかどうかで配置・向きを決定
        SetupPositionAndRotation(spawnPos, targetPos);

        if (keepTime > 0) Destroy(gameObject, keepTime);
    }

    private void SetupPositionAndRotation(Vector2 spawnPos, Vector2 targetPos)
    {
        if (!isMoving)
            //移動しないスキル：ターゲット（AimCursor）の位置にドンピシャで出す
            transform.position = targetPos;
        else
        {
            //移動するスキル：発生元に置いて、ターゲットの方向を向かせる
            transform.position = spawnPos;
            Vector2 direction = (targetPos - spawnPos).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    protected override void Update()
    {
        base.Update();

        // 移動処理
        if (isMoving)
            transform.position += transform.right * moveSpeed * Time.deltaTime;
    }

    protected override void OnHit(NetworkPlayer target)
    {
    }
}