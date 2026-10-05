// ========================================================
// 女教皇：HighPriestess
// ========================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// 女教皇（正位置）
/// </summary>
public class Arcana02HighPriestessFront : ArcanaLogic
{
    //だんだん攻撃力が上がる
    public const float timeInterval = 5f;
    public override void Execute(PlayerRoot player, Arcana sourceArcana) { player.StartCoroutine(OnUpdate(player, sourceArcana)); }
    public override IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
    {
        while (true)
        {
            yield return new WaitForSeconds(timeInterval);
            ArcanaNetworkManager.Instance.SetStatus(player.PlayerIndex.Value, StatusCategory.Atk, sourceArcana.GetKeepValue());
            NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
        }
    }
}

/// <summary>
/// 女教皇（逆位置）
/// </summary>
public class Arcana02HighPriestessBack : ArcanaLogic
{
    //だんだん攻撃力が下がる
    public const float normalAtkValue = 10f;
    public const float timeInterval = 5f;
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        //最初に攻撃力を上げる
        ArcanaNetworkManager.Instance.SetStatus(player.PlayerIndex.Value, StatusCategory.Atk, normalAtkValue);
        player.StartCoroutine(OnUpdate(player, sourceArcana));
    }
    public override IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
    {
        //時間経過で攻撃力を下げる
        while (true)
        {
            yield return new WaitForSeconds(timeInterval);
            ArcanaNetworkManager.Instance.SetStatus(player.PlayerIndex.Value, StatusCategory.Atk, sourceArcana.GetKeepValue());
            NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
        }
    }
}
