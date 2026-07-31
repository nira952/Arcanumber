using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// プレイヤー関係の処理を返すクラス
/// </summary>
public static class PlayerUtility
{
    // IDからプレイヤーを直接引くための辞書
    private static readonly Dictionary<int, NetworkPlayer> _playerMap = new Dictionary<int, NetworkPlayer>();

    /// <summary>
    /// 全プレイヤーのリストを返す
    /// </summary>
    public static List<NetworkPlayer> GetAllPlyer
    {
        get
        {
            // 登録済みのプレイヤー群からnull（破棄されたオブジェクト）を除外して返す
            CleanUpNullMapEntries();
            return _playerMap.Values.ToList();
        }
    }

    /// <summary>
    /// プレイヤーを追加する処理
    /// </summary>
    public static void RegisterPlayer(NetworkPlayer player)
    {
        if (player == null) return;

        int id = player.GetNetworkId();
        if (_playerMap.ContainsKey(id))
        {
            _playerMap[id] = player;
        }
        else
        {
            _playerMap.Add(id, player);
        }
    }

    /// <summary>
    /// プレイヤーを消す処理
    /// </summary>
    public static void UnregisterPlayer(int charaNo)
    {
        if (_playerMap.ContainsKey(charaNo))
        {
            _playerMap.Remove(charaNo);
        }
    }

    /// <summary>
    /// プレイヤーを探す処理（Dictionaryから即座に取得）
    /// </summary>
    public static NetworkPlayer FindPlayerByNo(int charaNo)
    {
        CleanUpNullMapEntries();
        if (_playerMap.TryGetValue(charaNo, out var player))
        {
            return player;
        }

        // 辞書に存在しない場合はGameManager側からの直接取得（フォールバック）を試みる
        return FindAndRegisterFromGameManager(charaNo);
    }

    /// <summary>
    /// 破棄されたインスタンス（Unityのnull）を辞書から取り除く内部処理
    /// </summary>
    private static void CleanUpNullMapEntries()
    {
        // Unityオブジェクト特有の Destroy 済み参照（== null）を掃除
        List<int> keysToRemove = null;
        foreach (var pair in _playerMap)
        {
            if (pair.Value == null)
            {
                keysToRemove ??= new List<int>();
                keysToRemove.Add(pair.Key);
            }
        }

        if (keysToRemove != null)
        {
            foreach (var key in keysToRemove)
            {
                _playerMap.Remove(key);
            }
        }
    }

    /// <summary>
    /// GameManagerから検索して自動登録する安全策（フォールバック）
    /// </summary>
    private static NetworkPlayer FindAndRegisterFromGameManager(int charaNo)
    {
        if (nira.Demo.GameManager.Instance == null) return null;

        var target = nira.Demo.GameManager.Instance.Players
            .Select(p => p != null ? p.GetComponentInChildren<NetworkPlayer>() : null)
            .FirstOrDefault(np => np != null && np.GetNetworkId() == charaNo);

        if (target != null)
        {
            RegisterPlayer(target);
        }

        return target;
    }

    /// <summary>
    /// 自分以外の全プレイヤーのリストを返す
    /// </summary>
    public static List<NetworkPlayer> GetOtherPlayers(NetworkPlayer self)
    {
        if (self == null) return GetAllPlyer;
        int selfId = self.GetNetworkId();

        return _playerMap.Values
            .Where(p => p != null && p.GetNetworkId() != selfId)
            .ToList();
    }

    /// <summary>
    /// プレイヤーの状態を更新するメソッド
    /// </summary>
    public static void UpdatePlayerSystem(NetworkPlayer player)
    {
        if (player == null) return;

        EffectDurationUpdate(player);
        ApplyPoisonDamage(player);
        CheckAndApplyRoofDamage(player);

        PlayerRoot root = player.GetPlayerController();
        if (root == null) return;

        EffectStopController(player, root);
        root.SetMoveSpeed(GetFinalSpeed(player));
    }

    private static void EffectStopController(NetworkPlayer player, PlayerRoot root)
    {
        bool canMove = true;
        bool canJump = true;
        bool reverseMove = false;
        bool canNAttack = true;

        if (HaveEffect(player, EffectList.Stun, false))
        {
            canMove = false;
            canJump = false;
        }
        if (HaveEffect(player, EffectList.Reverse, false))
            reverseMove = true;
        if (HaveEffect(player, EffectList.NoJump, false))
            canJump = false;
        if (!HaveEffect(player, EffectList.EnperorAura, true))
            canNAttack = false;

        root.SetIsMove(canMove);
        root.SetIsJump(canJump);
        root.SetIsChangeMove(reverseMove);
        root.SetIsNormalAttack(canNAttack);
    }

    private static void ApplyPoisonDamage(NetworkPlayer player)
    {
        List<EffectAbility> haveEffect = player.GetHaveEffect();
        foreach (EffectAbility e in haveEffect)
        {
            if (e == null) continue;
            Effect effectData = e.GetEffect();
            if (effectData == null) continue;

            if (effectData.GetEffectList() == EffectList.Poison && e.CheckDamageInterval())
                FinalDamage(player, e.GetValue());
        }
    }

    public static void CheckAndApplyRoofDamage(NetworkPlayer player)
    {
        EffectAbility sunEffect = player.GetHaveEffect().Find(e => e.GetEffect() != null && e.GetEffect().GetEffectList() == EffectList.SunBurn);
        if (sunEffect == null || sunEffect.GetEffect().GetIsUp() != false) return;

        Vector2 pos = player.transform.position;
        bool isUnderRoof = Physics2D.Raycast(pos + Vector2.up * 0.1f, Vector2.up, 50f, LayerMask.GetMask("Ground")).collider != null;

        if (!isUnderRoof && sunEffect.CheckDamageInterval())
            FinalDamage(player, sunEffect.GetValue());
    }

    private static void EffectDurationUpdate(NetworkPlayer player)
    {
        List<EffectAbility> haveEffect = player.GetHaveEffect();
        foreach (EffectAbility e in haveEffect)
        {
            if (e != null) e.DecreaseTime(Time.deltaTime);
        }

        player.CleanExpiredEffects();
    }

    public static Transform GetAimPos(NetworkPlayer player)
    {
        return player.GetPlayerController()?.GetAimCursor()?.GetTransform();
    }

    public static int GetCoolTimeIndex(int skillNo)
    {
        return (skillNo == GameConfig.SKILL_HOPPER_MAX) ? 5 : skillNo + 1;
    }

    public static bool HaveEffect(NetworkPlayer player, EffectList effect, bool isUp)
    {
        List<EffectAbility> haveEffect = player.GetHaveEffect();
        foreach (EffectAbility e in haveEffect)
        {
            if (e == null || e.GetEffect() == null) continue;
            if (e.GetEffect().GetEffectList() == effect && e.GetEffect().GetIsUp() == isUp)
                return true;
        }
        return false;
    }

    public static void ApplyEffectToOthers(NetworkPlayer self, EffectAbility effect)
    {
        List<NetworkPlayer> others = GetOtherPlayers(self);
        foreach (NetworkPlayer p in others)
        {
            p.SetHaveEffect(effect.Clone());
        }
    }

    public static float GetEffectValue(NetworkPlayer player, EffectList effect)
    {
        List<EffectAbility> haveEffect = player.GetHaveEffect();
        float Value = 1;
        foreach (EffectAbility e in haveEffect)
        {
            if (e == null || e.GetEffect() == null) continue;
            if (e.GetEffect().GetEffectList() == effect)
            {
                Value += (e.GetEffect().GetIsUp()) ? e.GetValue() : -e.GetValue();
            }
        }
        return Mathf.Max(0f, Value);
    }

    public static float GetFinalAtk(NetworkPlayer player)
    {
        float calculatedAtk = player.GetPlayerStatus().GetAtk() * GetEffectValue(player, EffectList.ATK);
        return Mathf.Max(0.1f, calculatedAtk);
    }

    public static float GetFinalAtk(NetworkPlayer player, Skill skill)
    {
        if (skill == null)
        {
            Debug.LogWarning("Skill is null in GetFinalAtk.");
            return Mathf.Max(0.1f, player != null ? player.GetPlayerStatus().GetAtk() * GetEffectValue(player, EffectList.ATK) : 0.1f);
        }

        if (player == null)
        {
            Debug.LogWarning("Player is null in GetFinalAtk.");
            return Mathf.Max(0.1f, skill.GetAtk());
        }

        float calculatedAtk = player.GetPlayerStatus().GetAtk()
            * skill.GetAtk()
            * GetEffectValue(player, EffectList.ATK);
        return Mathf.Max(0.1f, calculatedAtk);
    }

    public static float GetFinalDef(NetworkPlayer player)
    {
        float calculatedDef = player.GetPlayerStatus().GetDef() * GetEffectValue(player, EffectList.DEF);
        return Mathf.Max(0.1f, calculatedDef);
    }

    public static float GetFinalSpeed(NetworkPlayer player)
    {
        float calculatedSpeed = player.GetPlayerStatus().GetSpeed() * GetEffectValue(player, EffectList.DEX);
        return Mathf.Max(2f, calculatedSpeed);
    }

    public static void FinalDamage(NetworkPlayer target, NetworkPlayer player, float amount)
    {
        if (target == null) return;

        if (player != null && !HaveEffect(player, EffectList.IgnoreDefense, true))
            amount /= GetFinalDef(target);

        if (HaveEffect(target, EffectList.Counter, true) && player != null)
            ProcessCounterDamage(target, player, amount);

        if (HaveEffect(target, EffectList.Invincible, true))
            return;

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
        if (target == null) return;

        if (HaveEffect(target, EffectList.Invincible, true))
            return;

        amount /= GetFinalDef(target);
        if (amount < 0) amount = 0;

        if (HaveEffect(target, EffectList.Charm, true))
        {
            ProcessCharmDamage(target, amount);
            return;
        }

        target.TakeDamage(amount);
    }

    private static void ProcessCharmDamage(NetworkPlayer target, float amount)
    {
        List<NetworkPlayer> players = GetOtherPlayers(target);
        if (players.Count == 0) return;

        float shareDamage = (amount * 0.5f) / players.Count;
        foreach (NetworkPlayer p in players)
        {
            p?.TakeDamage(shareDamage);
        }
    }

    private static void ProcessCounterDamage(NetworkPlayer target, NetworkPlayer attacker, float amount)
    {
        float counterDamage = amount * 0.3f;
        attacker?.TakeDamage(counterDamage);
    }

    public static void FinalHeal(NetworkPlayer player, float amount)
    {
        if (player == null) return;

        List<NetworkPlayer> players = GetOtherPlayers(player);
        foreach (NetworkPlayer p in players)
        {
            if (p != null && HaveEffect(p, EffectList.HealSteal, true))
            {
                amount *= 0.5f;
                p.Heal(amount);
            }
        }
        player.Heal(amount);
    }

    public static float CoolTimeValue(NetworkPlayer player, float duration)
    {
        if (player == null) return duration;

        float effectMultiplier = GetEffectValue(player, EffectList.CoolTimeReduction);
        float reductionRate = 1.0f - (effectMultiplier - 1.0f);

        return Mathf.Max(0.1f, duration * reductionRate);
    }

}
