using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettingData", menuName = "Data/PlayerSettingData")]
public class PlayerSettingData : ScriptableObject
{
    public const string PlayerNameKey = "PlayerSettingData.PlayerName";
    public const string PlayerIdKey = "PlayerSettingData.PlayerId";

    public string playerName;
    public string playerId;

    public void Load()
    {
        playerName = PlayerPrefs.GetString(PlayerNameKey, playerName);
        playerId = PlayerPrefs.GetString(PlayerIdKey, playerId);
    }

    public void Save()
    {
        PlayerPrefs.SetString(PlayerNameKey, playerName ?? string.Empty);
        PlayerPrefs.SetString(PlayerIdKey, playerId ?? string.Empty);
        PlayerPrefs.Save();
    }

    public void InitializeIfNeeded()
    {
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = Guid.NewGuid().ToString();
        }
    }
}
