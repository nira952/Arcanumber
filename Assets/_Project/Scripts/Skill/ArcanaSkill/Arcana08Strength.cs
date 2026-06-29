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
    public const float attackValue = 1.2f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        float newAttack = player.GetPlayerStatus().GetAtk() * attackValue;
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
    public const float damageValue = 0.05f;
    private NetworkPlayer _owner;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        _owner = player;
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
                status.SetAtk(status.GetAtk() + damageValue * damage);
                break;
            case 1:
                status.SetDef(status.GetDef() + damageValue * damage);
                break;
            case 2:
                status.SetSpeed(status.GetSpeed() + damageValue * damage);
                _owner.GetPlayerController().SetMoveSpeed(status.GetSpeed());
                break;
        }
    }
}
