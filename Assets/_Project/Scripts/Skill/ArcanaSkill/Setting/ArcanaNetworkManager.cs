using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class ArcanaNetworkManager : NetworkBehaviour
{
    #region Singleton
    public static ArcanaNetworkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // オフラインモードかどうかを判定
        isLocalMode = PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsLocalMode;
    }
    #endregion


    bool isLocalMode = false;



    public void SetData(int index, Vector2 pos)
    {
        InstatiateEffectServerRpc(index, pos);
    }

    [ServerRpc]
    private void InstatiateEffectServerRpc(int index, Vector2 pos)
    {
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(index);

        GameObject effectObj = Instantiate(player.GetArcana().GetEffectPrefab(), pos, Quaternion.identity);

        if (!isLocalMode && IsServer)
        {
            if (effectObj.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn();
            }
        }

        effectObj.transform.SetParent(player.transform);

        Animator animator = effectObj.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            //アニメーションの長さでオブジェクトを消す
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Destroy(effectObj, stateInfo.length);
        }
    }

    /// <summary>
    /// ネットワーク経由でエフェクトをつける
    /// </summary>
    /// <param name="index">プレイヤー番号</param>
    /// <param name="isMine">自分かそれ以外か</param>
    /// <param name="effectIndex">エフェクト番号</param>
    /// <param name="isBuff">バフか</param>
    /// <param name="isDisplay">表示するか</param>
    /// <param name="time">時間</param>
    /// <param name="value">値</param>
    public void SetEffect(int index, bool isMine, int effectIndex, bool isBuff, bool isDisplay, float time, float value)
    {
        if (isMine)
            SetEffectServerRpc(index, effectIndex, isBuff, isDisplay, time, value);
        else
            SetEffectOtherServerRpc(index, effectIndex, isBuff, isDisplay, time, value);
    }

    /// <summary>
    /// 自分にエフェクトをつけるとき
    /// </summary>
    [ServerRpc]
    private void SetEffectServerRpc(int index, int effectIndex, bool isBuff, bool isDisplay, float time, float value)
    {
        Effect effect = EffectRegistry.Get((EffectList)effectIndex, isBuff);
        EffectAbility ea = new EffectAbility(effect, isDisplay, time, value);
        PlayerUtility.GetPlayerByIndex(index).AddEffect(ea.Clone());
    }

    /// <summary>
    /// 自分以外にエフェクトをつけるとき
    /// </summary>
    [ServerRpc]
    private void SetEffectOtherServerRpc(int index, int effectIndex, bool isBuff, bool isDisplay, float time, float value)
    {
        Effect effect = EffectRegistry.Get((EffectList)effectIndex, isBuff);
        EffectAbility ea = new EffectAbility(effect, isDisplay, time, value);
        PlayerUtility.ApplyEffectToOtherPlayers
            (PlayerUtility.GetPlayerByIndex(index), ea.Clone());
    }

    /// <summary>
    /// エフェクトを消す
    /// </summary>
    /// <param name="index"></param>
    public void ClearEffect(int index)
    {
        ClearEffectServerRpc(index);
    }

    [ServerRpc]
    private void ClearEffectServerRpc(int index)
    {
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(index);
        player.ClearAllEffects();
    }


    /// <summary>
    /// ネットワーク経由でステータスを変える
    /// </summary>
    /// <param name="index"></param>
    /// <param name="statusIndex"></param>
    /// <param name="value"></param>
    public void SetStatus(int index, StatusCategory statusIndex, float value)
    {
        switch (statusIndex)
        {
            case StatusCategory.Hp:
                SetHpServerRpc(index, value);
                break;
            case StatusCategory.Atk:
                SetAtkServerRpc(index, value);
                break;
            case StatusCategory.Def:
                SetDefServerRpc(index, value);
                break;
            case StatusCategory.Speed:
                SetSpeedServerRpc(index, value);
                break;
        }
    }

    /// <summary>
    /// 指定したプレイヤーの最大HPを指定倍率に基づいて更新する。
    /// </summary>
    private void SetHpServerRpc(int index, float value)
    {
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(index);
        float newHp = player.GetPlayerStatus().GetMaxHp() * value;
        player.GetPlayerStatus().SetMaxHp(newHp);
    }

    /// <summary>
    /// 攻撃力を上げる場合
    /// </summary>
    [ServerRpc]
    private void SetAtkServerRpc(int index, float value)
    {
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(index);
        float newAtk = player.GetPlayerStatus().GetAtk() * value;
        player.GetPlayerStatus().SetAtk(newAtk);
    }

    /// <summary>
    /// 防御力を上げる場合
    /// </summary>
    [ServerRpc]
    private void SetDefServerRpc(int index, float value)
    {
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(index);
        float newDef = player.GetPlayerStatus().GetDef() * value;
        player.GetPlayerStatus().SetDef(newDef);
    }

    /// <summary>
    /// 速度を上げる場合
    /// </summary>
    [ServerRpc]
    private void SetSpeedServerRpc(int index, float value)
    {
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(index);
        float newSpeed = player.GetPlayerStatus().GetSpeed() * value;
        player.GetPlayerStatus().SetSpeed(newSpeed);
    }

    /// <summary>
    /// ネットワーク経由で回復量を上げる場合   
    /// </summary>
    public void SetHeal(int index, float value)
    {
        HealServerRpc(index, value);
    }

    /// <summary>
    /// 回復量を上げる場合  
    /// </summary>
    /// <param name="index"></param>
    /// <param name="value"></param>
    [ServerRpc]
    private void HealServerRpc(int index, float value)
    {
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(index);
        player.ApplyHeal(value);
    }

    public void SetDamage(int index, bool isOther, float value)
    {
        if (isOther)
        {
            List<PlayerRoot> other = PlayerUtility.GetOtherPlayersIndex(index);
            for(int i = 0; i < other.Count; i++)
                DamageServerRpc(other[i].PlayerIndex.Value, value);
        }
        else
            DamageServerRpc(index, value);
    }

    [ServerRpc]
    private void DamageServerRpc(int index, float value)
    {
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(index);
        player.ApplyDamage(value);
    }
}

public enum StatusCategory
{
    Hp,
    Atk,
    Def,
    Speed
}