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
    private int cloneNum = 3;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        for(int i = 0; i < cloneNum; i++)
        {
            GameObject clone = GameObject.Instantiate(sourceArcana.GetEffectPrefab(), player.transform.position, Quaternion.identity);
            Object.Destroy(clone, 15f);
        }
    }

}

