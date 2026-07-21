// ========================================================
// 皇帝：Emperor
// ========================================================

/// <summary>
/// 皇帝（正位置）
/// </summary>
public class Arcana04EmperorFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //スキルを使用禁止にする
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
       Effect e = EffectRegistry.Get(EffectList.Silence, false);
       EffectAbility ea = new EffectAbility(e, false, sourceArcana.GetKeepValue(), 0);
       //とりあえず自分にかける
       player.SetHaveEffect(ea.Clone());
    }
}

/// <summary>
/// 皇帝（逆位置）
/// </summary>
public class Arcana04EmperorBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //通常攻撃が振れないが、ダメージが2倍になる
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        player.GetPlayerStatus().SetAtk(player.GetPlayerStatus().GetAtk() * 2);
        Effect e = EffectRegistry.Get(EffectList.EnperorAura, true);
        EffectAbility ea = new EffectAbility(e, false, -1, -1);
    }
}