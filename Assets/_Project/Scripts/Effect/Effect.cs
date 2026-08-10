using NaughtyAttributes;
using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ステータスエフェクトのクラス
/// </summary>
[CreateAssetMenu(fileName = "NewEffect", menuName = "ScriptableObjects/EffectData")]
public class Effect : ScriptableObject
{
    [Label("エフェクトの種類")][SerializeField] EffectList eList;
    [Label("エフェクトの画像")][SerializeField] Sprite effectImage;
    [Label("バフか")][SerializeField] bool isUp;
    public Effect(EffectList eList, Sprite effectImage, bool isUp)
    {
        this.eList = eList;
        this.effectImage = effectImage;
        this.isUp = isUp;
    }

    /**
     * --------- ゲッター ---------
     */
    public EffectList GetEffectList() { return eList; }
    public Sprite GetEffectImage() {  return effectImage; }
    public bool GetIsUp() { return isUp; }
}

[System.Serializable]
public class EffectAbility
{
    [SerializeField] Effect effect;
    [InspectorName("UIに表示するか")][SerializeField] bool isDisplay;
    [InspectorName("継続時間")][SerializeField] float time;
    [InspectorName("効果値")][SerializeField] float value;

    //毒用のタイマー
    [System.NonSerialized] private float lastDamageTime = -1f;

    public EffectAbility(Effect effect, bool isDisplay, float time, float value)
    {
        this.effect = effect;
        this.isDisplay = isDisplay;
        this.time = time;
        this.value = value;
    }

    /**
     * --------- ゲッター ---------
     */
    public Effect GetEffect() { return effect; }
    public bool IsDisplay() { return isDisplay; }
    public float GetTime() { return time; }
    public float GetValue() { return value; }

    /**
     * --------- セッター ---------
     */
    public void SetTime(float time) { this.time = time; }


    /// <summary>
    /// クローンの作成
    /// </summary>
    public EffectAbility Clone()
    {
        // 新しいインスタンスを作成し、現在の値をコピーして返す
        return new EffectAbility(this.effect, this.isDisplay, this.time, this.value);
    }

    /// <summary>
    /// 期限切れかどうか
    /// </summary>
    public bool IsExpired => time <= 0f && time != -1f;

    /// <summary>
    /// 時間を減らす処理
    /// </summary>
    public void DecreaseTime(float deltaTime)
    {
        if (time <= -1f) return;
        time -= deltaTime;
    }

    /// <summary>
    /// 毒ダメージの経過
    /// </summary>
    public bool CheckDamageInterval()
    {
        //まだ一度もダメージを与えていないなら、基準時間をセット
        if (lastDamageTime < 0)
        {
            lastDamageTime = Time.time;
            return false;
        }
        //基準時刻から1秒経過したかを確認
        if (Time.time - lastDamageTime >= 1.0f)
        {
            lastDamageTime += 1.0f;
            return true;
        }
        return false;
    }
}

/// <summary>
/// エフェクトの種類
/// </summary>
public enum EffectList
{
    [InspectorName("攻撃力")]ATK,
    [InspectorName("防御力")] DEF,
    [InspectorName("速度")] DEX,
    [InspectorName("毒")] Poison,
    [InspectorName("回復")] Heal,
    [InspectorName("無敵")] Invincible,
    [InspectorName("スキル使用禁止")] Silence,
    [InspectorName("ジャンプ禁止")] NoJump,
    [InspectorName("移動反転")] Reverse,
    [InspectorName("スタン")] Stun,
    [InspectorName("カウンター")] Counter,
    [InspectorName("クールタイム短縮")] CoolTimeReduction,
    [InspectorName("スキルタイム短縮")] SkillTimeReduction,
    [InspectorName("防御無視")] IgnoreDefense,
    [InspectorName("スキル巨大化")] SizeChange,
    [InspectorName("巨大化")] Giant,
    [InspectorName("縮小化")] Shrink,
    [InspectorName("幸運")] Lacky,
    [InspectorName("魅了")] Charm,
    [InspectorName("回復スティール")] HealSteal,
    [InspectorName("皇帝の威厳")] EnperorAura,
    [InspectorName("魂吸")] AtkHeal,
    [InspectorName("次元移動")] WallSwap,
    [InspectorName("太陽")] SunBurn
}

/// <summary>
/// ネットワーク経由でプレイヤー間同期するためのエフェクトデータ構造体
/// </summary>
public struct NetworkEffectData : INetworkSerializable, IEquatable<NetworkEffectData>
{
    public EffectList EffectType;   //エフェクトの種類
    public bool IsUp; //バフかデバフか
    public bool IsDisplay;  //UIに表示するか
    public float Time;  //継続時間
    public float Value; //効果値

    /// <summary>
    /// ネットワーク上でこのデータを送受信するためのメソッド
    /// </summary>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref EffectType);
        serializer.SerializeValue(ref IsUp);
        serializer.SerializeValue(ref IsDisplay);
        serializer.SerializeValue(ref Time);
        serializer.SerializeValue(ref Value);
    }

    /// <summary>
    /// 2つのエフェクトデータの内容が一致しているかを判定するメソッド
    /// </summary>
    public bool Equals(NetworkEffectData other)
    {
        return EffectType == other.EffectType &&
               IsUp == other.IsUp &&
               IsDisplay == other.IsDisplay &&
               Time == other.Time &&
               Value == other.Value;
    }
}