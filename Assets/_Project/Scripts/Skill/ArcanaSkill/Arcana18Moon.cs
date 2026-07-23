// ========================================================
// 月：Moon
// ========================================================

using UnityEngine;

/// <summary>
/// 月（正位置）
/// </summary>
public class Arcana18MoonFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //透明化する
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {

    }
}

/// <summary>
/// 月（逆位置）
/// </summary>
public class Arcana18MoonBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //分身を出す
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        for(int i = 0; i < sourceArcana.GetKeepValue(); i++)
        {
            GameObject clone = GameObject.Instantiate(sourceArcana.GetEffectPrefab(), player.transform.position, Quaternion.identity);
            AIPlayer ai = clone.GetComponent<AIPlayer>();

            clone.tag = "Player";

            if (i == 0)
                ai.SetIsHumanLike(true);

            ai.SetIsAIPlayer(true);
            Object.Destroy(clone, 15f);
        }
    }

}

