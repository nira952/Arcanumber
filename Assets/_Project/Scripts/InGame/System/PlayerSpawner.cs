using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// オフライン/オンライン両対応のプレイヤー生成管理クラス
/// </summary>
public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private bool isLocalMode = false; // オフラインモードフラグ
    [SerializeField] private Transform[] spawnPoints;   // 各プレイヤーの初期位置
    [SerializeField] private PlayerRoot playerPrefab;   // 生成するプレイヤーのプレハブ

    private void Start()
    {
        // PlayerDataManager が存在する場合はそこからモードを取得し、なければシリアライズされた isLocalMode を優先
        if (PlayerDataManager.Instance != null)
        {
            isLocalMode = PlayerDataManager.Instance.IsLocalMode;
        }

    }

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

            // プレイヤー名を設定
            string playerName = PlayerDataManager.Instance.GetPlayerNameByIndex(index);

            spawnedPlayer.gameObject.name = "Player" + index;

            // プレイヤーをサーバーリストに登録
            PlayerUtility.RegisterPlayer(spawnedPlayer);

            // カメラにプレイヤーを登録
            GameCameraManager.Instance.RegisterTarget(spawnedPlayer.transform);


            if (!IsOwner)
            {
                // プレイヤーのスキルとアルカナを取得
                Skill[] skills = PlayerDataManager.Instance.GetPlayerSkillsByIndex(index);
                Arcana arcana = PlayerDataManager.Instance.GetPlayerArcanaByIndex(index);

                // 自分以外のプレイヤーオブジェクトの場合、PlayerDataManagerから取得したArcanaとSkillを設定する
                spawnedPlayer.SetSkills(skills);
                spawnedPlayer.SetArcana(arcana);

            }

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
    public void SpawnAllPlayersOffline()
    {
        PlayerUtility.SetIsServer(true); // サーバーとしてのフラグを設定

        int playerIndex = 0;

        Transform spawnPoint = GetSpawnPoint(playerIndex);
        PlayerRoot spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        spawnedPlayer.PlayerIndex.Value = playerIndex; // プレイヤーのインデックスを設定

        PlayerUtility.RegisterPlayer(spawnedPlayer);

        PlayerOfflineController controller = spawnedPlayer.gameObject.AddComponent<PlayerOfflineController>();

        GameCameraManager.Instance.RegisterTarget(spawnedPlayer.transform);

        Debug.Log("[PlayerSpawner] PlayerOfflineController をアタッチし、オフライン生成を完了しました。");


        // オフラインモードでは、敵プレイヤーも生成する

        int enemyIndex = 1;
        Transform enemySpawnPoint = GetSpawnPoint(enemyIndex);
        PlayerRoot spawnedEnemy = Instantiate(playerPrefab, enemySpawnPoint.position, enemySpawnPoint.rotation);

        spawnedEnemy.PlayerIndex.Value = enemyIndex; // 敵プレイヤーのインデックスを設定

        PlayerUtility.RegisterPlayer(spawnedEnemy);

        PlayerAutoController enemyController = spawnedEnemy.gameObject.AddComponent<PlayerAutoController>();

        GameCameraManager.Instance.RegisterTarget(spawnedEnemy.transform);

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