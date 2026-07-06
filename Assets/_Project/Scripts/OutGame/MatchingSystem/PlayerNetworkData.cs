using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerNetworkData : INetworkSerializable, IEquatable<PlayerNetworkData>
{
    public int LobbyIndex;
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public ArcanaList arcana;
    public bool IsReady;

    // 内部的には byte 配列（または固定数）で保持して参照型（配列）を排除する
    // 要素4つなら、個別変数にするのがNGOでは一番軽くてバグが起きません
    private byte skill0;
    private byte skill1;
    private byte skill2;
    private byte skill3;


    /// <summary>
    /// 【バグ修正】指定した番号のスキル番号(int)を取得する
    /// </summary>
    public int GetSkill(int index) // 戻り値を ASkillCategory から int に修正
    {
        return index switch
        {
            0 => skill0,
            1 => skill1,
            2 => skill2,
            3 => skill3,
            _ => 0
        };
    }

    /// <summary>
    /// 【追加機能】RPCから直接送られてきた int（またはbyte）の配列からスキル番号を保存する
    /// </summary>
    public void SetSkillFromIds(int[] skillNos)
    {
        for (int i = 0; i < skillNos.Length && i < 4; i++)
        {
            byte bVal = (byte)skillNos[i];
            switch (i)
            {
                case 0: skill0 = bVal; break;
                case 1: skill1 = bVal; break;
                case 2: skill2 = bVal; break;
                case 3: skill3 = bVal; break;
            }
        }
    }


    /// <summary>
    /// 自分のスキルをネットワーク上に保存
    /// </summary>
    /// <param name="skills"></param>
    public void SetSkill(Skill[] skills)
    {
        for (int i = 0; i < skills.Length && i < 4; i++)
        {
            byte bVal = (byte)skills[i].GetSkillNo();

            switch(i)
                {
                case 0:
                    skill0 = bVal;
                    break;
                case 1:
                    skill1 = bVal;
                    break;
                case 2:
                    skill2 = bVal;
                    break;
                case 3:
                    skill3 = bVal;
                    break;
            }
        }

    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref LobbyIndex);
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref skill0);
        serializer.SerializeValue(ref skill1);
        serializer.SerializeValue(ref skill2);
        serializer.SerializeValue(ref skill3);
    }
    public bool Equals(PlayerNetworkData other)
    {
        return LobbyIndex == other.LobbyIndex &&
               ClientId == other.ClientId &&
               PlayerName.Equals(other.PlayerName) &&
               arcana == other.arcana &&
               IsReady == other.IsReady && 
               skill0 == other.skill0 &&
               skill1 == other.skill1 &&
               skill2 == other.skill2 &&
               skill3 == other.skill3;
    }
}