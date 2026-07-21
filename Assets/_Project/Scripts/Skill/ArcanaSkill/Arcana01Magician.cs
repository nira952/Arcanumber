// ========================================================
// 魔術師：Magician
// ========================================================

using UnityEngine;

/// <summary>
/// 魔術師（正位置）
/// </summary>
public class Arcana01MagicianFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //防御無視
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        Effect e = EffectRegistry.Get(EffectList.IgnoreDefense, false);
        EffectAbility ea = new EffectAbility(e, false, -1f, -1f);
        player.SetHaveEffect(ea.Clone());
    }
}

/// <summary>
/// 魔術師（逆位置）
/// </summary>
public class Arcana01MagicianBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //自分のサポートをしてくれる敵を召喚する
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        for (int i = 0; i < (int)sourceArcana.GetKeepValue(); i++)
        {
            GameObject monster = GameObject.Instantiate(sourceArcana.GetEffectPrefab());
            monster.transform.position = player.transform.position;
        }
    }
}
