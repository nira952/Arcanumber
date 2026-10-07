// ========================================================
// 力：Strength
// ========================================================

using Unity.Services.Lobbies.Models;
using UnityEngine;

/// <summary>
/// 力（正位置）
/// </summary>
public class Arcana08StrengthFront : ArcanaLogic
{
    //攻撃力が上がる
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetStatus(player.PlayerIndex.Value, StatusCategory.Atk, player.GetPlayerStatus().GetAtk() * sourceArcana.GetKeepValue());
    }
}

/// <summary>
/// 力（逆位置）
/// </summary>
public class Arcana08StrengthBack : ArcanaLogic
{
    //等価交換
    private PlayerRoot _owner;
    private Arcana sArcana;
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        _owner = player;
        sArcana = sourceArcana;

        OnDamageReceived(_owner, 0f);
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
    private void OnDamageReceived(PlayerRoot target, float damage)
    {
        //自分自身へのダメージでなければ無視
        if (target != _owner) return;
        //攻撃・防御・速度のどれか一つをランダムに選ぶ (0:攻撃, 1:防御, 2:速度)
        int choice = Random.Range(0, 3);
        PlayerStatus status = target.GetPlayerStatus();

        //ステータスを強化
        switch (choice)
        {
            case 0:
                ArcanaNetworkManager.Instance.SetStatus(target.PlayerIndex.Value, StatusCategory.Atk, status.GetAtk() + sArcana.GetKeepValue() * damage);
                break;
            case 1:
                ArcanaNetworkManager.Instance.SetStatus(target.PlayerIndex.Value, StatusCategory.Def, status.GetDef() + sArcana.GetKeepValue() * damage);
                break;
            case 2:
                ArcanaNetworkManager.Instance.SetStatus(target.PlayerIndex.Value, StatusCategory.Speed, status.GetSpeed() + sArcana.GetKeepValue() * damage);
                break;
        }

        ArcanaNetworkManager.Instance.SetAnimation(target.PlayerIndex.Value, target.transform.position, 0.5f);

    }
}
