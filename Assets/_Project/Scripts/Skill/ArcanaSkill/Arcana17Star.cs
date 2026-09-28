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
    //あなたはスターだ
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        player.StartCoroutine(OnUpdate(player, sourceArcana));
    }
    public override IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
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
    //無敵
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.Invincible, true);
        EffectAbility ea = new EffectAbility(e, true, sourceArcana.GetKeepValue(), -1f);
        player.AddEffect(ea.Clone());
    }

}
