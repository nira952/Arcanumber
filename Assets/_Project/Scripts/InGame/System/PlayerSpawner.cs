using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// オフライン/オンライン両対応のプレイヤー生成管理クラス
/// </summary>
public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;   // 各プレイヤーの初期位置
    [SerializeField] private PlayerRoot playerPrefab;   // 生成するプレイヤーのプレハブ

    /// <summary>
    /// 現在接続されているすべてのクライアントのプレイヤーを一括生成・登録する (サーバー専用)
    /// </summary>
    public void SpawnAllPlayersOnline()
    {
        // 接続されているクライアントの ID を取得
        var connectedClientIds = NetworkManager.Singleton.ConnectedClientsIds;

        Debug.Log($"[PlayerSpawner] 全プレイヤーの一括生成を開始します。現在の接続人数: {connectedClientIds.Count}人");

        PlayerUtility.SetIsServer(true); // サーバーとしてのフラグを設定

        int index = 0; // 0～3のインデックスを順番に割り当てる

        foreach (ulong clientId in connectedClientIds)
        {
            // インデックスや ClientId に応じてスポーン位置を決定
            Transform spawnPoint = GetSpawnPoint(index);

            // プレイヤーを生成
            PlayerRoot spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // プレイヤーのインデックスを設定
            spawnedPlayer.SetPlayerIndex(index);

            // プレイヤー名を設定
            string playerName = PlayerDataManager.Instance.GetPlayerNameByIndex(index);

            spawnedPlayer.gameObject.name = "Player" + index;

            if (spawnedPlayer.TryGetComponent<NetworkObject>(out var networkObj))
            {
                // 1. スポーン処理（これは全端末へ伝播する）
                networkObj.SpawnAsPlayerObject(clientId);
            }
            else
            {
                Debug.LogError($"[PlayerSpawner] プレイヤーオブジェクトに NetworkObject コンポーネントが見つかりません。ClientId: {clientId}, PlayerName: {playerName}");
            }

            index++;
        }
    }

    /// <summary>
    /// オフライン（単体テスト等）用にプレイヤーを生成・初期化する
    /// </summary>
    public void SpawnAllPlayersOffline(bool isTutorial)
    {
        PlayerUtility.SetIsServer(true); // サーバーとしてのフラグを設定

        int playerIndex = 0;

        Transform spawnPoint = GetSpawnPoint(playerIndex);
        PlayerRoot spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        spawnedPlayer.SetPlayerIndex(playerIndex); // プレイヤーのインデックスを設定

        PlayerOfflineController controller = spawnedPlayer.gameObject.AddComponent<PlayerOfflineController>();

        // チュートリアルモードの場合、プレイヤーの操作を止める
        controller.IsStop = isTutorial; 

        Debug.Log("[PlayerSpawner] PlayerOfflineController をアタッチし、オフライン生成を完了しました。");


        // オフラインモードでは、敵プレイヤーも生成する

        int enemyIndex = 1;
        Transform enemySpawnPoint = GetSpawnPoint(enemyIndex);
        PlayerRoot spawnedEnemy = Instantiate(playerPrefab, enemySpawnPoint.position, enemySpawnPoint.rotation);

        spawnedEnemy.SetPlayerIndex(enemyIndex); // 敵プレイヤーのインデックスを設定

        PlayerAutoController enemyController = spawnedEnemy.gameObject.AddComponent<PlayerAutoController>();

        Debug.Log("[PlayerSpawner] PlayerAutoController をアタッチし、オフライン敵生成を完了しました。");
    }

    /// <summary>
    /// インデックスに応じたスポーン地点を取得する
    /// </summary>
    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[Mathf.Abs(index) % spawnPoints.Length];
        }

        return transform;
    }
}