// ========================================================
// 愚者：Fool
// ========================================================

/// <summary>
/// 愚者（正位置）
/// </summary>
public class Arcana00FoolFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //HPを1.3倍にする
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        float newHp = player.GetPlayerStatus().GetMaxHp() * sourceArcana.GetKeepValue();
        player.GetPlayerStatus().SetMaxHp(newHp);
    }
}

/// <summary>
/// 愚者（逆位置）
/// </summary>
public class Arcana00FoolBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.DeathEffect;
    //何も変わらない
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {

    }
}
