using UnityEngine;

public class Arcana17Star : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //ランダムな効果を持った星が流れてくる
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        //攻撃力減少、防御力減少、速度減少、毒、スキル使用禁止
        //ジャンプ禁止、スタン、ダメージ、移動反転
        
    }

}
