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
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        if (player == null) return;

        player.StartCoroutine(OnUpdate(player, sourceArcana));
    }

    //コルーチン本体
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        Transform playerTransform = player.transform;

        // 1. パラメータの取得
        // KeepValue を「移動距離」、あるいは別の列を「移動時間」にするなど調整可能です
        float distance = sourceArcana.GetKeepValue(); // 例: 10f (進む距離)
        if (distance <= 0f) distance = 8f;

        float duration = 0.2f; // 高速移動にかかる時間（秒）- 短いほど一瞬（高速）になる
        float elapsed = 0f;

        Vector3 startPos = playerTransform.position;
        Vector3 direction = playerTransform.forward;
        Vector3 targetPos = startPos + (direction * distance);

        // エフェクト再生（開始時）
        PlayArcanaVisuals(player, sourceArcana, playerTransform);

        // CharacterControllerがあれば一時的にオフにする（すり抜けや位置干渉を防ぐため）
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 指定した時間（duration）かけて滑らかに高速移動させる
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // イージング（最初は速く、後半減速する等）をつけたい場合は Mathf.SmoothStep なども使えます
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
    public override void Execute(NetworkPlayer player, Arcana sourceArcana)
    {
        for(int i = 0; i < sourceArcana.GetKeepValue(); i++)
        {
            GameObject clone = GameObject.Instantiate(sourceArcana.GetEffectPrefab(), player.transform.position, Quaternion.identity);
            AIPlayer ai = clone.GetComponent<AIPlayer>();

            clone.tag = "Player";

            if (i == 0)
                ai.SetIsHumanLike(true);

            ai.SetIsAIPlayer(true);
            Object.Destroy(clone, 15f);
        }
    }

}

