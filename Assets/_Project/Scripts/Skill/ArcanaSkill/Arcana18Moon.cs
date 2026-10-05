// ========================================================
// 月：Moon
// ========================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// 月（正位置）
/// </summary>
public class Arcana18MoonFront : ArcanaLogic
{
    //ブリンクをする
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        if (player == null) return;

        player.StartCoroutine(OnUpdate(player, sourceArcana));
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
    }

    public override IEnumerator OnUpdate(PlayerRoot player, Arcana sourceArcana)
    {
        Transform playerTransform = player.transform;

        //パラメータの取得
        float distance = sourceArcana.GetKeepValue();
        if (distance <= 0f) distance = 8f;

        float duration = 0.2f; //高速移動にかかる時間
        float elapsed = 0f;

        Vector3 startPos = playerTransform.position;

        //Visualオブジェクトから向いている方向を取得する
        float facingDir = 1f;
        Transform visualTransform = playerTransform.Find("Visual");

        if (visualTransform != null)
            facingDir = visualTransform.localScale.x >= 0f ? -1f : 1f;
        else
            facingDir = playerTransform.localScale.x >= 0f ? -1f : 1f;

        Vector3 direction = new Vector3(facingDir, 0f, 0f);
        Vector3 targetPos = startPos + (direction * distance);

        //エフェクト再生
        PlayArcanaVisuals(player, sourceArcana, playerTransform);

        //PlayerRoot（親）にアタッチされている BoxCollider2D を取得する
        BoxCollider2D boxCol = player.GetComponent<BoxCollider2D>();

        //BoxCastAll を使って自分自身を除外しつつ壁を検知する
        if (boxCol != null)
        {
            Vector2 origin = boxCol.bounds.center;
            Vector2 size = boxCol.size;
            float angle = playerTransform.eulerAngles.z;

            //進行方向にあるすべてのコライダーを取得
            RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, angle, direction, distance);

            foreach (var hit in hits)
            {
                if (hit.collider != null)
                {
                    //自分自身のコライダーや子オブジェクトはスキップ
                    if (hit.collider.transform.IsChildOf(playerTransform)) continue;

                    //ヒットしたのが Wall または Ground の場合
                    if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Ground"))
                    {
                        //一番手前にある壁までの安全な距離を計算し、目標地点をそこに上書きしてループを抜ける
                        float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
                        targetPos = startPos + (direction * safeDistance);
                        break;
                    }
                }
            }
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        //計算された安全な targetPos まで滑らかに移動させる
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            playerTransform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        // 最終位置にしっかり合わせる
        playerTransform.position = targetPos;

        if (cc != null) cc.enabled = true;
    }
}

/// <summary>
/// 月（逆位置）
/// </summary>
public class Arcana18MoonBack : ArcanaLogic
{
    //分身を出す
    public override void Execute(PlayerRoot player, Arcana sourceArcana)
    {
        NetWorkAudioManager.Instance.PlayGlobal(sourceArcana.GetSE());
        for (int i = 0; i < sourceArcana.GetKeepValue(); i++)
        {
            //sourceArcana.GetEffectPrefab().GetComponent<SpriteRenderer>().sprite =
                //player.GetComponent<SpriteRenderer>().sprite;
            SkillManager.Instance.SpawnRpcObject(player.PlayerIndex.Value);
        }
    }

}

