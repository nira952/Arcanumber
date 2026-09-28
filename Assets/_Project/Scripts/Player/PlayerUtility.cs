using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// プレイヤー関係の処理を返すクラス
/// </summary>
public static class PlayerUtility
{
    private static readonly List<PlayerRoot> playerList = new List<PlayerRoot>();

    private static bool isServer = false;

    public static void SetIsServer(bool value)
    {
        isServer = value;
    }

    private static bool IsServer()
    {
        if(isServer)
        {
            return true;
        }
        else
        {
            Debug.LogWarning("[PlayerUtility] サーバー権限がありません");
            return false;
        }

    }

    public static void RegisterPlayer(PlayerRoot playerRoot)
    {
        if (!IsServer()){ return; }

        if (playerRoot == null) return;

        playerList.Add(playerRoot);
    }


    public static void UnregisterPlayer(PlayerRoot playerRoot)
    {
        if (!IsServer()) { return; }

        if (playerRoot == null) return;
        playerList.Remove(playerRoot);
    }
    /// <summary>
    /// 全ての PlayerRoot オブジェクトの一覧を返す。
    /// </summary>
    public static List<PlayerRoot> GetAllPlayer()
    {
        if (!IsServer()) { return null; }

        return playerList.Where(root => root != null).ToList();
    }

    /// <summary>
    /// プレイヤーのルートを番号で探す処理
    /// </summary>
    public static PlayerRoot GetPlayerByIndex(int index)
    {
        if (!IsServer()) { return null; }

        return playerList.FirstOrDefault(root => root != null && root.PlayerIndex.Value == index);
    }

    /// <summary>
    /// 指定したプレイヤー以外の PlayerRoot オブジェクトの一覧を返す。
    /// </summary>
    public static List<PlayerRoot> GetOtherPlayers(PlayerRoot self)
    {
        if (!IsServer()) { return null; }

        if (self == null) return GetAllPlayer();
        return playerList.Where(root => root != null && root != self).ToList();
    }

    /// <summary>
    /// 指定したプレイヤー以外にエフェクトを付与する
    /// </summary>
    public static void ApplyEffectToOtherPlayers(PlayerRoot self, EffectAbility effect)
    {
        if (!IsServer()) { return; }
        List<PlayerRoot> otherPlayers = GetOtherPlayers(self);
        foreach (PlayerRoot player in otherPlayers)
        {
            player?.AddEffect(effect);
        }
    }

    /// <summary>
    /// 指定したプレイヤー以外にダメージを与える
    /// </summary>
    /// <param name="self"></param>
    /// <param name="damage"></param>
    public static void ApplyDamageToOtherPlayers(PlayerRoot self, float damage)
    {
        if (!IsServer()) { return; }
        List<PlayerRoot> otherPlayers = GetOtherPlayers(self);
        foreach (PlayerRoot player in otherPlayers)
        {
            player?.ApplyDamage(damage);
        }
    }

    public static void FinalDamage(int targetIndex, int attackerIndex, float finalDmg)
    {
        // 対象プレイヤーと攻撃者プレイヤーを取得
        PlayerRoot attackPlayer = GetPlayerByIndex(attackerIndex);
        PlayerRoot targetPlayer = GetPlayerByIndex(targetIndex);

        if (attackPlayer == null || targetPlayer == null) return;

        float defPow = targetPlayer.GetCurrentDefense();


        // 防御無視バフを攻撃側が持っている場合、防御力を無視してダメージ計算
        if (!attackPlayer.HaveEffect(EffectList.IgnoreDefense, true))
            finalDmg /= defPow;

        // カウンター効果を持っている場合、攻撃者に反射ダメージを与える
        if (targetPlayer.HaveEffect(EffectList.Counter, true))
            ProcessCounterDamage(targetPlayer, attackPlayer, finalDmg);

        // 無敵効果がある場合、ダメージを与えない
        if (targetPlayer.HaveEffect(EffectList.Invincible, true))
            return;

        // チャーム効果がある場合、ダメージを他のプレイヤーに分散させる
        if (targetPlayer.HaveEffect(EffectList.Charm, true))
        {
            ProcessCharmDamage(targetPlayer, finalDmg);
            return;
        }

        // 最小ダメージを0に設定
        if (finalDmg < 0) finalDmg = 0;

        // ダメージを適用
        targetPlayer.ApplyDamage(finalDmg);
    }


    private static void ProcessCharmDamage(PlayerRoot target, float amount)
    {
        List<PlayerRoot> players = GetOtherPlayers(target);
        if (players.Count == 0) return;

        float shareDamage = (amount * 0.5f) / players.Count;
        foreach (PlayerRoot p in players)
        {
            p?.ApplyDamage(shareDamage);
        }
    }

    private static void ProcessCounterDamage(PlayerRoot target, PlayerRoot attacker, float amount)
    {
        float counterDamage = amount * 0.3f;
        attacker?.ApplyDamage(counterDamage);
    }

}
