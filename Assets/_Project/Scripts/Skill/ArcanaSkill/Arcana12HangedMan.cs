// ========================================================
// つるされた男：Hanged Man
// ========================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// つるされた男（正位置）
/// </summary>
public class Arcana12HangedManFront : ArcanaLogic
{
    //カメラが反転する
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        player.StartCoroutine(OnUpdate(player, sourceArcana));
    }
    public override IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
    {
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
        GameCameraManager.Instance.SetReversed(true);
        //アルカナの効果持続時間、もしくは必要な時間だけ待機
        yield return new WaitForSeconds(sourceArcana.GetKeepValue());
        GameCameraManager.Instance.SetReversed(false);
    }
}

/// <summary>
/// つるされた男（逆位置）
/// </summary>
public class Arcana12HangedManBack : ArcanaLogic 
{
    //運の確立を最大まで上げる
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.Lacky, true);
        EffectAbility ea = new EffectAbility(e, false, -1, -1);
        player.AddEffect(ea.Clone());
    }

}



