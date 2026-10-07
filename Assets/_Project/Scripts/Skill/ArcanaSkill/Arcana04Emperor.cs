// ========================================================
// 皇帝：Emperor
// ========================================================

/// <summary>
/// 皇帝（正位置）
/// </summary>
public class Arcana04EmperorFront : ArcanaLogic
{
    //スキルを使用禁止にする
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, false, 
           (int)EffectList.Silence, false, false, sourceArcana.GetKeepValue(), 0);
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
}

/// <summary>
/// 皇帝（逆位置）
/// </summary>
public class Arcana04EmperorBack : ArcanaLogic
{
    //通常攻撃が振れないが、ダメージが2倍になる
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        ArcanaNetworkManager.Instance.SetStatus(player.PlayerIndex.Value, StatusCategory.Atk, player.GetPlayerStatus().GetAtk() * 2);
        ArcanaNetworkManager.Instance.SetEffect(player.PlayerIndex.Value, false,
          (int)EffectList.EnperorAura, true, false, sourceArcana.GetKeepValue(), -1f);
    }
}