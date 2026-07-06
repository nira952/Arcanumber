using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤー関係の処理を返すクラス
/// </summary>
public static class PlayerUtility
{
    //プレイヤーをまとめる辞書
    private static Dictionary<int, NetworkPlayer> playerCache = new Dictionary<int, NetworkPlayer>();

    /// <summary>
    /// プレイヤーを追加する処理
    /// </summary>
    public static void RegisterPlayer(NetworkPlayer player)
    {
        if (!playerCache.ContainsKey(player.GetNetworkId()))
            playerCache.Add(player.GetNetworkId(), player);
    }

    /// <summary>
    ///プレイヤーを消す処理
    /// </summary>
    public static void UnregisterPlayer(int charaNo)
    {
        if (playerCache.ContainsKey(charaNo))
            playerCache.Remove(charaNo);
    }

    /// <summary>
    /// プレイヤーを探す処理
    /// </summary>
    public static NetworkPlayer FindPlayerByNo(int charaNo)
    {
        if (playerCache.TryGetValue(charaNo, out NetworkPlayer player))
            return player;
        return null;
    }

    /// <summary>
    /// 自分以外の全プレイヤーのリストを返す
    /// </summary>
    public static List<NetworkPlayer> GetOtherPlayers(NetworkPlayer self)
    {
        List<NetworkPlayer> others = new List<NetworkPlayer>();
        foreach (var entry in playerCache)
        {
            //IDが自分自身と一致しないものだけを追加
            if (entry.Key != self.GetNetworkId())
            {
                others.Add(entry.Value);
            }
        }
        return others;
    }

    /// <summary>
    /// プレイヤーの状態を更新するメソッド
    /// </summary>
    public static void UpdatePlayerSystem(NetworkPlayer player)
    {
        //プレイヤーとコントローラーの存在チェック
        if (player == null) return;
        //エフェクト更新
        EffectDurationUpdate(player);
        //持続ダメージ用
        ApplyPoisonDamage(player);
        //太陽用
        CheckAndApplyRoofDamage(player);

        PlayerController controller = player.GetPlayerController();
        if (controller == null) return;

        //エフェクトによる行動制限の処理
        EffectStopController(player, controller);

        //移動速度の計算
        controller.SetMoveSpeed(GetFinalSpeed(player));
        //位置の更新
        controller.LatePlayerPosUpdate();
    }

    /// <summary>
    /// 行動制限の処理をするメソッド
    /// </summary>
    private static void EffectStopController(NetworkPlayer player, PlayerController controller)
    {
        bool canMove = true;
        bool canJump = true;
        bool reverseMove = false;
        bool canNAttack = true;

        //スタン状態の場合、動けないようにする
        if (HaveEffect(player, EffectList.Stun, false))
        {
            canMove = false;
            canJump = false;
        }
        //混乱状態の場合、移動方向を逆にする
        if (HaveEffect(player, EffectList.Reverse, false))
            reverseMove = true;
        //ジャンプ禁止状態の場合、ジャンプできないようにする
        if (HaveEffect(player, EffectList.NoJump, false))
            canJump = false;
        //皇帝の威厳がある場合
        if(!HaveEffect(player, EffectList.EnperorAura, true))
            canNAttack = false;

        //結果をコントローラーに反映
        controller.SetIsMove(canMove);
        controller.SetIsJump(canJump);
        controller.SetIsChangeMove(reverseMove);
        controller.SetIsNormalAttack(canNAttack);

    }

    /// <summary>
    /// 持続用のダメージメソッド
    /// </summary>
    private static void ApplyPoisonDamage(NetworkPlayer player)
    {
        List<EffectAbility> haveEffect = player.GetHaveEffect();
        foreach (EffectAbility e in haveEffect)
        {
            if (e == null) continue;
            Effect effectData = e.GetEffect();
            if (effectData == null) continue;
            // 毒効果があり、かつ1秒経過したタイミングなら
            if (effectData.GetEffectList() == EffectList.Poison && e.CheckDamageInterval())
                FinalDamage(player, e.GetValue());
        }
    }

    /// <summary>
    /// 太陽のダメージメソッド
    /// </summary>
    public static void CheckAndApplyRoofDamage(NetworkPlayer player)
    {
        //太陽のデバフを取得
        EffectAbility sunEffect = player.GetHaveEffect().Find(e => e.GetEffect().GetEffectList() == EffectList.SunBurn);
        //エフェクトがない
        if (sunEffect == null || sunEffect.GetEffect().GetIsUp() != false) return;

        //屋根判定
        Vector2 pos = player.transform.position;
        bool isUnderRoof = Physics2D.Raycast(pos + Vector2.up * 0.1f, Vector2.up, 50f, LayerMask.GetMask("Ground")).collider != null;

        //屋根がなく、かつ1秒経過しているならダメージ
        if (!isUnderRoof && sunEffect.CheckDamageInterval())
            FinalDamage(player, sunEffect.GetValue());
    }

    /// <summary>
    /// 効果の残り時間を更新するメソッド
    /// </summary>
    private static void EffectDurationUpdate(NetworkPlayer player)
    {
        List<EffectAbility> haveEffect = player.GetHaveEffect();
        foreach (EffectAbility e in haveEffect)
        {
            e.DecreaseTime(Time.deltaTime);
        }

        //player自身に期限切れを送る
        player.CleanExpiredEffects();
    }

    /// <summary>
    /// 現在のエイムの場所を返す
    /// </summary>
    public static Transform GetAimPos(NetworkPlayer player)
    {
        return player.GetPlayerController().GetAimCursor().GetTransform();
    }

    /// <summary>
    /// プレイヤーの現在のスキル選択番号から、クールタイム配列のインデックス(1~5)を計算する
    /// </summary>
    public static int GetCoolTimeIndex(int skillNo)
    {
        return (skillNo == GameConfig.SKILL_HOPPER_MAX) ? 5 : skillNo + 1;
    }

    /// <summary>
    /// 特定の効果を持っているかどうかを返す
    /// </summary>
    public static bool HaveEffect(NetworkPlayer player, EffectList effect, bool isUp)
    {
        List<EffectAbility> haveEffect = player.GetHaveEffect();
        foreach(EffectAbility e in haveEffect)
        {
            if (e == null || e.GetEffect() == null) continue;
            if (e.GetEffect().GetEffectList() == effect && e.GetEffect().GetIsUp() == isUp)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 自分以外の全プレイヤーにエフェクトを付与する
    /// </summary>
    public static void ApplyEffectToOthers(NetworkPlayer self, EffectAbility effect)
    {
        List<NetworkPlayer> others = GetOtherPlayers(self);
        foreach (NetworkPlayer p in others)
        {
            // NetworkPlayer側にエフェクト追加用のメソッドがある想定です
            p.SetHaveEffect(effect.Clone());
        }
    }

    /// <summary>
    /// バフデバフ倍率の計算をするメソッド
    /// </summary>
    public static float GetEffectValue(NetworkPlayer player, EffectList effect)
    {
        List<EffectAbility> haveEffect = player.GetHaveEffect();
        float Value = 1;
        foreach (EffectAbility e in haveEffect)
        {
            if (e.GetEffect().GetEffectList() == effect)
            {
                Value += (e.GetEffect().GetIsUp()) ? e.GetValue() : -e.GetValue();
            }
        }
        return Mathf.Max(0f, Value);
    }

    /// <summary>
    /// 最終的な攻撃力メソッド（最低値: 0.1f）
    /// </summary>
    public static float GetFinalAtk(NetworkPlayer player)
    {
        float calculatedAtk = player.GetPlayerStatus().GetAtk() * GetEffectValue(player, EffectList.ATK);
        return Mathf.Max(0.1f, calculatedAtk);
    }
    public static float GetFinalAtk(NetworkPlayer player, Skill skill)
    {
        //スキルを含めたダメージ計算
        float calculatedAtk = player.GetPlayerStatus().GetAtk()
            * skill.GetAtk()
            * GetEffectValue(player, EffectList.ATK);
        return Mathf.Max(0.1f, calculatedAtk);
    }

    /// <summary>
    /// 最終的な防御力メソッド（最低値: 0.1f）
    /// </summary>
    public static float GetFinalDef(NetworkPlayer player)
    {
        float calculatedDef = player.GetPlayerStatus().GetDef() * GetEffectValue(player, EffectList.DEF);
        return Mathf.Max(0.1f, calculatedDef);
    }

    /// <summary>
    /// 最終的な移動速度メソッド（最低値: 2.0f）
    /// </summary>
    public static float GetFinalSpeed(NetworkPlayer player)
    {
        float calculatedSpeed = player.GetPlayerStatus().GetSpeed() * GetEffectValue(player, EffectList.DEX);
        return Mathf.Max(2f, calculatedSpeed);
    }
    
    /// <summary>
    /// 最終的なダメージ計算
    /// </summary>
    public static void FinalDamage(NetworkPlayer target, NetworkPlayer player, float amount)
    {
        //カウンター状態か
        if (HaveEffect(target, EffectList.Counter, true))
            ProcessCounterDamage(target, player, amount);
        //防御無視か
        if (!HaveEffect(player, EffectList.IgnoreDefense, true))
            amount /= GetFinalDef(target);
        //ダメージ無効を持っているか
        if (HaveEffect(target, EffectList.Invincible, true))
            return;
        //魅了を持っているか
        if (HaveEffect(target, EffectList.Charm, true))
        {
            ProcessCharmDamage(target, amount);
            return;
        }
        if (amount < 0) amount = 0;
        target.TakeDamage(amount);
    }
    public static void FinalDamage(NetworkPlayer target, float amount)
    {
        if (HaveEffect(target, EffectList.Invincible, true))
            return;
        amount /= GetFinalDef(target);
        if (amount < 0) amount = 0;
        //魅了を持っているか
        if (HaveEffect(target, EffectList.Charm, true))
        {
            ProcessCharmDamage(target, amount);
            return;
        }
        target.TakeDamage(amount);
    }

    /// <summary>
    /// 魅了による肩代わり処理
    /// </summary>
    private static void ProcessCharmDamage(NetworkPlayer target, float amount)
    {
        List<NetworkPlayer> players = GetOtherPlayers(target);
        if (players.Count == 0) return;

        float shareDamage = (amount * 0.5f) / players.Count;
        foreach (NetworkPlayer p in players)
            p.TakeDamage(shareDamage);
    }

    /// <summary>
    /// カウンター処理
    /// </summary>
    private static void ProcessCounterDamage(NetworkPlayer target, NetworkPlayer attacker, float amount)
    {
        //0.3倍のダメージを算出
        float counterDamage = amount * 0.3f;
        //攻撃者にダメージを与える
        attacker.TakeDamage(counterDamage);
    }

    /// <summary>
    /// ヒールの最終計算
    /// </summary>
    public static void FinalHeal(NetworkPlayer player, float amount)
    {
        //回復スティール持っているプレイヤーがいたら
        List<NetworkPlayer> players = GetOtherPlayers(player);
        foreach (NetworkPlayer p in players)
        {
            //半分回復を横取りする
            if(HaveEffect(p, EffectList.HealSteal, true))
                amount *= 0.5f;
        }
        player.Heal(amount);
    }

    /// <summary>
    /// クールタイムの数値計算メソッド
    /// </summary>
    public static float CoolTimeValue(NetworkPlayer player, float duration)
    {
        //倍率計算
        float effectMultiplier = GetEffectValue(player, EffectList.CoolTimeReduction);
        //短縮率（倍率）を計算
        float reductionRate = 1.0f - (effectMultiplier - 1.0f);

        //マイナスにならないよう
        return Mathf.Max(0.1f, duration * reductionRate);
    }
}
