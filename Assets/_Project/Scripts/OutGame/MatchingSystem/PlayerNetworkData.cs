using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum PlayerClassType : byte
{
    Attacker,
    Defender,
    Supporter,
    Speedster
}

public struct PlayerNetworkData : INetworkSerializable, IEquatable<PlayerNetworkData>
{
    public int LobbyIndex;
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public Color SelectedColor;

    // Enumの例
    public PlayerClassType ClassType;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref LobbyIndex);
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref SelectedColor);

        // 通信時には byte にキャストして送受信する
        if (serializer.IsReader)
        {
            // 読み込み（受信）時：一度 byte として受け取ってから enum にキャスト
            byte rawValue = 0;
            serializer.SerializeValue(ref rawValue);
            ClassType = (PlayerClassType)rawValue;
        }
        else
        {
            // 書き込み（送信）時：enum を byte にキャストして送る
            byte rawValue = (byte)ClassType;
            serializer.SerializeValue(ref rawValue);
        }
    }

    public bool Equals(PlayerNetworkData other)
    {
        return LobbyIndex == other.LobbyIndex &&
               ClientId == other.ClientId &&
               ClassType == other.ClassType; // 比較対象にも追加
    }
}