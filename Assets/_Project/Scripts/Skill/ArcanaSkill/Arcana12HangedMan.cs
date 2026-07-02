using System.Collections;
using UnityEngine;

/// <summary>
/// つるされた男（正位置）
/// </summary>
public class Arcana12HangedManFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //カメラが反転する
    private float valueTime = 5f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        player.StartCoroutine(OnUpdate(player, sourceArcana));
    }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        GameCameraManager.Instance.SetReversed(true);
        //アルカナの効果持続時間、もしくは必要な時間だけ待機
        yield return new WaitForSeconds(valueTime);
        GameCameraManager.Instance.SetReversed(false);
    }
}

/// <summary>
/// つるされた男（逆位置）
/// </summary>
public class Arcana12HangedManBack : ArcanaLogic 
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //運の確立を最大まで上げる
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.Lacky, true);
        EffectAbility ea = new EffectAbility(e, false, -1, -1);
        player.SetHaveEffect(ea.Clone());
    }

}



