using UnityEngine;

/// <summary>
/// プレイヤーがスキルやAimCursorを使って発動するオブジェクト
/// </summary>
public class SkillObject : MagicObject
{
    // スキルの挙動タイプ
    private bool isMoving = false;
    private float moveSpeed = 0f;
    private SkillBehaviorType behaviorType;

    // ホーミング用のターゲット
    private Transform targetEnemy;

    // ブーメラン挙動用の変数
    private float boomerangTimer = 0f;
    private bool isReturning = false;
    private Transform ownerTransform;

    public void Initialize(int charaNo, Skill skill, Vector2 targetPos)
    {
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(charaNo);
        float finalDmg = PlayerUtility.GetFinalAtk(player, skill);

        Vector2 spawnPos = player != null ? (Vector2)player.transform.position : (Vector2)transform.position;

        behaviorType = skill.GetBehaviorType();
        keepTime = skill.GetKeepTime();
        moveSpeed = skill.GetMoveSpeed();
        isPenetrate = skill.GetIsPenetrate();
        reflectCount = skill.GetReflectCount();
        useAnimationEndEvent = skill.GetEffectAnimation() != null;
        effect = skill.GetEffect();
        se = skill.GetSe();

        CommonInitialize(charaNo, finalDmg);

        //挙動タイプに応じた初期化・配置
        SkillBehaviorSelectType(behaviorType, spawnPos, targetPos);

        if (keepTime > 0) Destroy(gameObject, keepTime);
    }

    /// <summary>
    /// 挙動タイプごとに個別の初期化メソッドを呼び出す
    /// </summary>
    private void SkillBehaviorSelectType(SkillBehaviorType type, Vector2 spawnPos, Vector2 targetPos)
    {
        switch (type)
        {
            case SkillBehaviorType.Straight:
                SetupStraight(spawnPos, targetPos);
                break;

            case SkillBehaviorType.TargetPosition:
            case SkillBehaviorType.Stationary:
            case SkillBehaviorType.Area:
                SetupStationary(targetPos);
                break;

            case SkillBehaviorType.Homing:
                SetupHoming(spawnPos, targetPos);
                break;

            case SkillBehaviorType.Boomerang:
                SetupBoomerang(spawnPos, targetPos);
                break;

            default:
                SetupStraight(spawnPos, targetPos);
                break;
        }
    }

    // ==========================================
    // 各挙動の初期化・セットアップ
    // ==========================================

    private void SetupStraight(Vector2 spawnPos, Vector2 targetPos)
    {
        isMoving = (moveSpeed > 0);
        transform.position = spawnPos;

        Vector2 direction = (targetPos - spawnPos).normalized;
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void SetupStationary(Vector2 targetPos)
    {
        isMoving = false;
        transform.position = targetPos;
    }

    /// <summary>
    /// ホーミング処理の初期化
    /// </summary>
    private void SetupHoming(Vector2 spawnPos, Vector2 targetPos)
    {
        isMoving = true;
        transform.position = spawnPos;

        //「Enemy」タグのオブジェクトから一番近いものを探す
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float minDistance = float.MaxValue;
        Transform nearest = null;

        foreach (var enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemy.transform;
            }
        }
        targetEnemy = nearest;

        Vector2 direction = (targetPos - spawnPos).normalized;
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// ブーメラン処理の初期化
    /// </summary>
    private void SetupBoomerang(Vector2 spawnPos, Vector2 targetPos)
    {
        isMoving = true;
        transform.position = spawnPos;
        boomerangTimer = 0f;
        isReturning = false;

        NetworkPlayer player = PlayerUtility.FindPlayerByNo(haveCharaNo);
        if (player != null) ownerTransform = player.transform;

        Vector2 direction = (targetPos - spawnPos).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // ==========================================
    // 毎フレームの更新処理
    // ==========================================

    protected override void Update()
    {
        base.Update();

        if (!isMoving) return;

        // 挙動タイプごとに毎フレームの制御を分岐
        switch (behaviorType)
        {
            case SkillBehaviorType.Homing:
                UpdateHoming();
                break;

            case SkillBehaviorType.Boomerang:
                UpdateBoomerang();
                break;
        }

        // 基本の移動処理（毎フレーム前方に進む）
        transform.position += transform.right * moveSpeed * Time.deltaTime;
    }

    private void UpdateHoming()
    {
        if (targetEnemy == null) return;

        Vector2 dir = (targetEnemy.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, 300f * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, 0, newAngle);
    }

    private void UpdateBoomerang()
    {
        boomerangTimer += Time.deltaTime;

        if (!isReturning && boomerangTimer > 0.8f)
        {
            isReturning = true;
        }

        if (isReturning && ownerTransform != null)
        {
            Vector2 dir = (ownerTransform.position - transform.position).normalized;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            float currentAngle = transform.eulerAngles.z;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, 400f * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, newAngle);

            // 手元に戻ったら消滅
            if (Vector2.Distance(transform.position, ownerTransform.position) < 0.5f)
            {
                Destroy(gameObject);
            }
        }
    }
}