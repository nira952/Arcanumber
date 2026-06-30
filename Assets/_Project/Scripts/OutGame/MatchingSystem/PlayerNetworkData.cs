using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerNetworkData : INetworkSerializable, IEquatable<PlayerNetworkData>
{
    public int LobbyIndex;
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public ArcanaList arcana;

    // 内部的には byte 配列（または固定数）で保持して参照型（配列）を排除する
    // 要素4つなら、個別変数にするのがNGOでは一番軽くてバグが起きません
    private byte skill0;
    private byte skill1;
    private byte skill2;
    private byte skill3;

    
    /// <summary>
    /// 指定した番号のスキルを取得する
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public ASkillCategory GetSkill(int index)
    {
        return index switch
        {
            0 => (ASkillCategory)skill0,
            1 => (ASkillCategory)skill1,
            2 => (ASkillCategory)skill2,
            3 => (ASkillCategory)skill3,
            _ => default
        };
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

        // arcana の処理（省略）

        // byte型なので、キャストなしでそのまま超高速にシリアライズ可能
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
               skill0 == other.skill0 &&
               skill1 == other.skill1 &&
               skill2 == other.skill2 &&
               skill3 == other.skill3; // これで完全な値比較が可能になります
    }
}