// ========================================================
// 悪魔：Devil
// ========================================================

using UnityEngine;

/// <summary>
/// 悪魔（正位置）
/// </summary>
public class Arcana15DevilFront : ArcanaLogic
{
    //通常攻撃にデバフがつく
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        float value = 1.3f;
        ArcanaNetworkManager.Instance.SetStatus(player.PlayerIndex.Value, StatusCategory.Def, player.GetPlayerStatus().GetDef() * value);
    }
}

/// <summary>
/// 悪魔（逆位置）
/// </summary>
public class Arcana15DevilBack : ArcanaLogic
{
    //デバフ・バフをすべて解除し、その数だけ攻撃力を上げる
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        //デバフ、バフの数を数える
        int efeNum = player.GetAllEffectCount();
        //すべて消す
        ArcanaNetworkManager.Instance.ClearEffect(player.PlayerIndex.Value);
        ArcanaNetworkManager.Instance.SetStatus(player.PlayerIndex.Value, StatusCategory.Atk, player.GetPlayerStatus().GetAtk() * efeNum * sourceArcana.GetKeepValue());
        ArcanaNetworkManager.Instance.SetAnimation(player.PlayerIndex.Value, player.transform.position, 0.5f);
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
}

