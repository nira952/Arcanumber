using UnityEngine;

/// <summary>
/// トラップスキルのクラス
/// </summary>
public class TrapObject : MagicObject
{
    public override void Initialize(int charaNo, Skill skill, Vector2 pos)
    {
        base.Initialize(charaNo, skill, pos);
        //移動なし、持続ダメージなし、アニメ終了で消す
        SetMovement(false, 0f, false, true);
        // 最後に位置と回転を確定させる
        SetupPositionAndRotation(pos);
    }

    protected override void OnHit(NetworkPlayer target)
    {
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(haveCharaNo);
        //ダメージを与える
        PlayerUtility.FinalDamage(target, player, dmg);

        //トラップ固有のアニメーション再生
        if (animator != null)
        {
            animator.Play("Trap");
        }
    }
}