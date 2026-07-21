// ========================================================
// 星：Star
// ========================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// 星（正位置）
/// </summary>
public class Arcana17StarFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //あなたはスターだ
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        player.StartCoroutine(OnUpdate(player, sourceArcana));
    }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        GameCameraManager.Instance.SetZoom(true, player.transform);
        yield return new WaitForSeconds(sourceArcana.GetKeepValue());
        GameCameraManager.Instance.SetZoom(false);
    }
}

/// <summary>
/// 星（逆位置）
/// </summary>
public class Arcana17StarBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //無敵
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.Invincible, true);
        EffectAbility ea = new EffectAbility(e, true, sourceArcana.GetKeepValue(), -1f);
        player.SetHaveEffect(ea.Clone());
    }

}
