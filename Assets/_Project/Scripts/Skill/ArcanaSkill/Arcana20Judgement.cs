using UnityEngine;

public class Arcana20JudgementFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //当たると現在体力が２５％減る
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {

    }

}

public class Arcana20JudgementBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //銃弾の雨が降る
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {

    }

}

