using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

// ========================================================
// 運命の輪：Wheel of Fortune
// ========================================================

/// <summary>
/// 運命の輪（正位置）
/// </summary>
public class Arcana10WheelOfFortuneFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //ランダムテレポート
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        //安全な場所を探す
        Vector2 safePos = FindSafePosition(player);

        //テレポート実行
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        player.transform.position = safePos;
    }
    /// <summary>
    /// テレポート先を探す処理
    /// </summary>
    private Vector2 FindSafePosition(NetworkPlayer player)
    {
        CapsuleCollider2D cap = player.GetComponent<CapsuleCollider2D>();
        Camera cam = Camera.main;
        int groundMask = LayerMask.GetMask("Ground");

        //カメラの表示範囲内
        Vector2 min = cam.ViewportToWorldPoint(new Vector3(0.05f, 0.05f, 0));
        Vector2 max = cam.ViewportToWorldPoint(new Vector3(0.95f, 0.95f, 0));

        for (int i = 0; i < 20; i++)
        {
            Vector2 targetPos = new Vector2(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y)
            );

            //床や壁にめり込んでないか
            if (Physics2D.OverlapCapsule(targetPos + cap.offset, cap.size, cap.direction, 0, groundMask) == null)
            {
                return targetPos;
            }
        }

        return player.transform.position;
    }
}


/// <summary>
/// 運命の輪（逆位置）
/// </summary>
public class Arcana10WheelOfFortuneBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    private float noJumpDuration = 10f;
    //ジャンプができないようにする
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.NoJump, false);
        EffectAbility ea = new EffectAbility(e, true, noJumpDuration, 0);
        //自分以外にかける
        PlayerUtility.ApplyEffectToOthers(player, ea);
    }
}
