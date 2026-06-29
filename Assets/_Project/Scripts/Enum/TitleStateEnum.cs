using TMPro;
using UnityEngine;

/// <summary>
/// タイトルの状態のEnum
/// </summary>
public enum TitleStateEnum
{
    Loading,
    Lobby, 
    Room, 
    RoomList,
    CreateRoom, 
    NameInput, 
    Setting,
    Error
}

/// <summary>
/// 現在接続中のプレイヤーリスト
/// </summary>
[System.Serializable]
public class PlayerUIItem
{
    public GameObject uiObject;
    public TextMeshProUGUI nameText;
}
