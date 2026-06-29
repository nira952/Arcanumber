using System.Collections;
using UnityEngine;

/// <summary>
/// つるされた男（正位置）
/// </summary>
public class Arcana12HangedManFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    private float valueTime = 5f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        player.StartCoroutine(OnUpdate(player, sourceArcana));
    }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        //カメラを入れる
        Camera cam = Camera.main;
        if (cam == null) yield break;
        //反転する
        Matrix4x4 flipMatrix = Matrix4x4.Scale(new Vector3(1, -1, 1));
        cam.projectionMatrix = cam.projectionMatrix * flipMatrix;

        //アルカナの効果持続時間、もしくは必要な時間だけ待機
        yield return new WaitForSeconds(valueTime);
        //元に戻す
        cam.ResetProjectionMatrix();
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



