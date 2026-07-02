using System.Collections;
using UnityEngine;

public class Arcana17StarFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //あなたはスターだ
    private float viewTime = 15f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        player.StartCoroutine(OnUpdate(player, sourceArcana));
    }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        GameCameraManager.Instance.SetZoom(true, player.transform);
        yield return new WaitForSeconds(viewTime);
        GameCameraManager.Instance.SetZoom(false);
    }
}

public class Arcana17StarBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //ランダムな効果を持った星が流れてくる
    private Color[] starColors =
        {
        };
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        //攻撃力減少、防御力減少、速度減少、毒、スキル使用禁止
        //ジャンプ禁止、スタン、ダメージ、移動反転
        
    }

}
