using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class WallSwap : NetworkBehaviour
{
    //クールダウン中かどうかを管理する辞書
    private static Dictionary<PlayerRoot, bool> isCoolingDown = new Dictionary<PlayerRoot, bool>();
    private float coolTime = 0.5f; //クールダウン時間
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        //プレイヤーを探す
        PlayerRoot player = collision.gameObject.GetComponent<PlayerRoot>();
        if (player == null) return;

        //クールダウン中なら処理を中断
        if (isCoolingDown.ContainsKey(player) && isCoolingDown[player]) return;

        if (player.HaveEffect(EffectList.WallSwap, true))
        {
            MovePositionClientRpc(player.PlayerIndex.Value);
            //クールダウン開始
            StartCoroutine(WarpCooldown(player));
        }

    }

    [ClientRpc]
    private void MovePositionClientRpc(int index)
    {
        GameObject player = PlayerUtility.GetPlayerByIndex(index).gameObject;

        //ワープ処理
        player.transform.position = new Vector3(
            -player.transform.position.x, player.transform.position.y, player.transform.position.z);
    }

    //クールダウン処理
    private IEnumerator WarpCooldown(PlayerRoot player)
    {
        isCoolingDown[player] = true;
        //0.5秒間はワープを無効化する
        yield return new WaitForSeconds(coolTime);
        isCoolingDown[player] = false;
    }
}