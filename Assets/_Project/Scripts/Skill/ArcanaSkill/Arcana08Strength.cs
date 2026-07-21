// ========================================================
// 力：Strength
// ========================================================

using UnityEngine;

/// <summary>
/// 力（正位置）
/// </summary>
public class Arcana08StrengthFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //攻撃力が上がる
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        float newAttack = player.GetPlayerStatus().GetAtk() * sourceArcana.GetKeepValue();
        player.GetPlayerStatus().SetAtk(newAttack);
    }
}

/// <summary>
/// 力（逆位置）
/// </summary>
public class Arcana08StrengthBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.DamageEffect;
    //等価交換
    private NetworkPlayer _owner;
    private Arcana sArcana;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        _owner = player;
        sArcana = sourceArcana;
        NetworkPlayer.OnTakeDamageEvent += OnDamageReceived;
    }
    private void OnDamageReceived(NetworkPlayer target, float damage)
    {
        //自分自身へのダメージでなければ無視
        if (target != _owner) return;
        //攻撃・防御・速度のどれか一つをランダムに選ぶ (0:攻撃, 1:防御, 2:速度)
        int choice = Random.Range(0, 3);
        PlayerStatus status = _owner.GetPlayerStatus();

        //ステータスを強化
        switch (choice)
        {
            case 0:
                status.SetAtk(status.GetAtk() + sArcana.GetKeepValue() * damage);
                break;
            case 1:
                status.SetDef(status.GetDef() + sArcana.GetKeepValue() * damage);
                break;
            case 2:
                status.SetSpeed(status.GetSpeed() + sArcana.GetKeepValue() * damage);
                _owner.GetPlayerController().SetMoveSpeed(status.GetSpeed());
                break;
        }
    }
}
