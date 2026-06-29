// ========================================================
// 恋人：Lovers
// ========================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// 恋人（正位置）
/// </summary>
public class Arcana06LoversFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //魅了状態にする
    public const float timeValue = 5f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.Charm, true);
        EffectAbility ea = new EffectAbility(e, true, timeValue, -1f);
        player.SetHaveEffect(ea.Clone());
    }
}

/// <summary>
/// 恋人（逆位置）
/// </summary>
public class Arcana06LoversBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //画面を暗くする
    public const float timeInterval = 10f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana) { player.StartCoroutine(OnUpdate(player, sourceArcana)); }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        //画面を暗くするエフェクトを再生
        VisualEffectManager.Instance.StartCoroutine(
            VisualEffectManager.Instance.ShowDarkPanel(true));
        yield return new WaitForSeconds(timeInterval);
        //画面を暗くするエフェクトを再生
        VisualEffectManager.Instance.StartCoroutine(
            VisualEffectManager.Instance.ShowDarkPanel(false));
    }
}
