using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // これが必要

public class WallSwap : MonoBehaviour
{
    private static Dictionary<PlayerRoot, bool> isCoolingDown = new Dictionary<PlayerRoot, bool>();
    private float coolTime = 0.5f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[WallSwap] 衝突検知: {collision.gameObject.name}");

        PlayerRoot player = collision.gameObject.GetComponent<PlayerRoot>();
        if (player == null) return;

        if (isCoolingDown.ContainsKey(player) && isCoolingDown[player]) return;

        if (player.HaveEffect(EffectList.WallSwap, true))
        {
            Vector2 newPos = new Vector2(-player.transform.position.x, player.transform.position.y);

            // ArcanaNetworkManager に新しく追加した TransformServerRpc を直接叩く
            ArcanaNetworkManager.Instance.SetTransform(player.PlayerIndex.Value, newPos);

            StartCoroutine(WarpCooldown(player));
        }
    }

    private IEnumerator WarpCooldown(PlayerRoot player)
    {
        isCoolingDown[player] = true;
        yield return new WaitForSeconds(coolTime);
        isCoolingDown[player] = false;
    }
}