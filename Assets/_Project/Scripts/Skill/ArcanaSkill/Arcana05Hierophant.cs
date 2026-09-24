// ========================================================
// 教皇：Hierophant
// ========================================================

/// <summary>
/// 教皇（正位置）
/// </summary>
public class Arcana05HierophantFront : ArcanaLogic
{
    //スキルの当たり判定がすこし大きくなる
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        EffectAbility effect = new EffectAbility(
            EffectRegistry.Get(EffectList.SizeChange, true),
            false, -1, sourceArcana.GetKeepValue());
        player.SetHaveEffect(effect.Clone());
    }
}

/// <summary>
/// 教皇（逆位置）
/// </summary>
public class Arcana05HierophantBack : ArcanaLogic
{
    //左右移動が反転
    public override void Execute(NetworkPlayer player, Arcana sourceArcana) 
    {
        Effect e = EffectRegistry.Get(EffectList.Reverse, false);
        EffectAbility ea = new EffectAbility(e, false, sourceArcana.GetKeepValue(), 0);
        //とりあえず自分にかける
        player.SetHaveEffect(ea.Clone());
    }
}
