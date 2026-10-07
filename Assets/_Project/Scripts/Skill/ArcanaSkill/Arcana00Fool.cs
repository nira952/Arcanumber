// ========================================================
// 愚者：Fool
// ========================================================

/// <summary>
/// 愚者（正位置）
/// </summary>
public class Arcana00FoolFront : ArcanaLogic
{
    //HPを1.3倍にする
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetStatus(player.PlayerIndex.Value, StatusCategory.Hp, player.GetPlayerStatus().GetMaxHp() * sourceArcana.GetKeepValue());
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
}

/// <summary>
/// 愚者（逆位置）
/// </summary>
public class Arcana00FoolBack : ArcanaLogic
{
    //何も変わらない
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {

    }
}
