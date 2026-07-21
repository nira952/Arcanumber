// ========================================================
// 悪魔：Devil
// ========================================================

using UnityEngine;

/// <summary>
/// 悪魔（正位置）
/// </summary>
public class Arcana15DevilFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //10秒は死なない
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {

    }
}

/// <summary>
/// 悪魔（逆位置）
/// </summary>
public class Arcana15DevilBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //デバフ・バフをすべて解除し、その数だけ攻撃力を上げる
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        //デバフ、バフの数を数える
        int efeNum = player.GetHaveEffect().Count;
        //すべて消す
        BattleUIManager.Instance.RemoveStatusUI(player, player.GetHaveEffect());
        player.GetHaveEffect().Clear();
        //数だけ攻撃力を上げる
        player.GetPlayerStatus().SetAtk(
            player.GetPlayerStatus().GetAtk() + player.GetPlayerStatus().GetAtk() * efeNum * sourceArcana.GetKeepValue());
    }
}

