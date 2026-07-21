using System.Collections;
using UnityEngine;

public class Arcana20JudgementFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //当たると現在体力が２５％減る
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {

    }
}

public class Arcana20JudgementBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.Command;
    //銃弾の雨が降る
    public float length = 15f;  //範囲
    Vector2 pos = new Vector2(0, 7.5f);
    public float spawnInterval = 0.03f; //銃弾を出す間隔
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        player.StartCoroutine(OnUpdate(player, sourceArcana));
    }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        float elapsed = 0f;

        while (elapsed < sourceArcana.GetKeepValue())
        {
            //範囲内でランダムなX
            float randomX = Random.Range(-length / 2f, length / 2f);
            //生成位置（高さは固定7.5f）
            Vector3 spawnPos = new Vector3(pos.x + randomX, pos.y, 0);

            //生成（回転は必要に応じて調整）
            GameObject obj = Object.Instantiate(sourceArcana.effectPrefab, spawnPos, Quaternion.Euler(0, 0, -90));
            Bullet magic = obj.GetComponent<Bullet>();
            if (magic != null)
                //初期化
                magic.Initialize(player.GetNetworkId(), null, spawnPos);

            //次の弾までの待機時間
            yield return new WaitForSeconds(spawnInterval);
            //秒数加算
            elapsed += spawnInterval;
        }
    }
}

