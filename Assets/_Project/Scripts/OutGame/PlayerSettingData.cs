using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct PlayerInfo : INetworkSerializable, System.IEquatable<PlayerInfo>
{
    public FixedString64Bytes Name;
    public FixedString64Bytes Id;
    public int LobbyIndex;

    /// <summary>
    /// ネットワーク上でのシリアライズ処理を行う
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serializer"></param>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Id);
        serializer.SerializeValue(ref LobbyIndex);
    }

    /// <summary>
    /// 他のPlayerInfoと等しいかどうかを判定する
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(PlayerInfo other)
    {
        return Name == other.Name && Id == other.Id && LobbyIndex == other.LobbyIndex;
    }

}

[CreateAssetMenu(fileName = "PlayerSettingData", menuName = "Data/PlayerSettingData")]
public class PlayerSettingData : ScriptableObject
{
    public const string PlayerNameKey = "PlayerSettingData.PlayerName";
    public const string PlayerIdKey = "PlayerSettingData.PlayerId";

    public PlayerInfo playerInfo;


    public void Load()
    {
        playerInfo.Name = PlayerPrefs.GetString(PlayerNameKey, playerInfo.Name.ToString());
        playerInfo.Id = PlayerPrefs.GetString(PlayerIdKey, playerInfo.Id.ToString());
    }

    public void Save()
    {
        PlayerPrefs.SetString(PlayerNameKey, playerInfo.Name.ToString() ?? string.Empty);
        PlayerPrefs.SetString(PlayerIdKey, playerInfo.Id.ToString() ?? string.Empty);
        PlayerPrefs.Save();
    }

    public void InitializeIfNeeded()
    {
        if (string.IsNullOrEmpty(playerInfo.Id.ToString()   ))
        {
            playerInfo.Id = Guid.NewGuid().ToString();
        }
    }

}
