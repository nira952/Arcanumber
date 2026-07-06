using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WallSwap : MonoBehaviour
{
    //クールダウン中かどうかを管理する辞書
    private static Dictionary<NetworkPlayer, bool> isCoolingDown = new Dictionary<NetworkPlayer, bool>();
    private float coolTime = 0.5f; //クールダウン時間
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //プレイヤーを探す
        NetworkPlayer player = collision.gameObject.GetComponent<NetworkPlayer>();
        if (player == null) return;

        //クールダウン中なら処理を中断
        if (isCoolingDown.ContainsKey(player) && isCoolingDown[player]) return;

        if (PlayerUtility.HaveEffect(player, EffectList.WallSwap, true))
        {
            //ワープ処理
            player.transform.position = new Vector3(
                -player.transform.position.x, player.transform.position.y, player.transform.position.z);
            //クールダウン開始
            StartCoroutine(WarpCooldown(player));
        }
    }

    //クールダウン処理
    private IEnumerator WarpCooldown(NetworkPlayer player)
    {
        isCoolingDown[player] = true;
        //0.5秒間はワープを無効化する
        yield return new WaitForSeconds(coolTime);
        isCoolingDown[player] = false;
    }
}