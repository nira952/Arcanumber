using System.Collections;
using UnityEngine;

public class Arcana20JudgementFront : ArcanaLogic
{
    //当たると現在体力が３０％減る
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        SkillManager.Instance.SpawnRpcObject(player.PlayerIndex.Value);
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
}

public class Arcana20JudgementBack : ArcanaLogic
{
    //銃弾の雨が降る

    public float spawnInterval = 0.03f; //銃弾を出す間隔
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        player.StartCoroutine(OnUpdate(player, sourceArcana));
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }
    public override IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
    {
        float elapsed = 0f;

        while (elapsed < sourceArcana.GetKeepValue())
        {
            SkillManager.Instance.SpawnRpcObject(player.PlayerIndex.Value);

            //次の弾までの待機時間
            yield return new WaitForSeconds(spawnInterval);
            //秒数加算
            elapsed += spawnInterval;
        }
    }
}

