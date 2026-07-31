using nira.Demo;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// オフライン/オンライン両対応のプレイヤー生成管理クラス
/// </summary>
public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private bool isLocalMode = false; // デバッグ・オフラインモードフラグ
    [SerializeField] private Transform[] spawnPoints;   // 各プレイヤーの初期位置
    [SerializeField] private PlayerRoot playerPrefab;   // 生成するプレイヤーのプレハブ

    private void Start()
    {
        // PlayerDataManager が存在する場合はそこからモードを取得し、なければシリアライズされた isLocalMode を優先
        if (PlayerDataManager.Instance != null)
        {
            isLocalMode = PlayerDataManager.Instance.IsLocalMode;
        }

        // デバッグモード（オフライン）の場合、OnNetworkSpawn が呼ばれないため Start で直接生成する
        if (isLocalMode)
        {
            Debug.Log("[PlayerSpawner] オフラインモードでプレイヤーを単体生成します。");
            SpawnPlayerOffline();
        }
    }

    public override void OnNetworkSpawn()
    {
        // オフラインデバッグ時はネットワーク生成処理をスキップ
        if (isLocalMode) return;

        // サーバー（ホスト）側のみが、シーンロード完了イベントを監視する
        if (IsServer)
        {
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }
    }
    public override void OnNetworkDespawn()
    {
        // イベントハンドラの解除（メモリリーク・多重登録防止）
        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        }
    }


    /// <summary>
    /// シーンイベントのハンドラ（サーバー専用）
    /// </summary>
    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        // イベントが「全クライアントのロード完了 (LoadEventCompleted)」かつ「現在のシーン」の場合のみ実行
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            // 該当シーンロードイベントを発生させたのが自シーンかチェック
            if (sceneEvent.SceneName == gameObject.scene.name)
            {
                Debug.Log($"[PlayerSpawner] 全クライアントのシーンロードが完了しました。一括生成を開始します。");

                // 二重実行を防ぐため、一度実行したらイベント解除
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;

                SpawnAllPlayers();
            }
        }
    }
    /// <summary>
    /// 現在接続されているすべてのクライアントのプレイヤーを一括生成・登録する (サーバー専用)
    /// </summary>
    private void SpawnAllPlayers()
    {
        var connectedClientIds = NetworkManager.Singleton.ConnectedClientsIds;
        Debug.Log($"[PlayerSpawner] 全プレイヤーの一括生成を開始します。現在の接続人数: {connectedClientIds.Count}人");

        int index = 0;
        foreach (ulong clientId in connectedClientIds)
        {
            // インデックスや ClientId に応じてスポーン位置を決定
            Transform spawnPoint = GetSpawnPoint(index);
            PlayerRoot spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // プレイヤー名を設定（デバッグ用）
            string playerName = $"Player_{index}";
            if (IsServer)
            {
                playerName += "Host";
            }
            spawnedPlayer.gameObject.name = playerName;

            if (spawnedPlayer.TryGetComponent<NetworkObject>(out var networkObj))
            {
                // 1. スポーン処理（これは全端末へ伝播する）
                networkObj.SpawnAsPlayerObject(clientId);


                // 2. サーバー側のみ GameManager に登録する
                if (IsServer)
                {
                    GameManager.Instance.RegisterPlayer(spawnedPlayer);
                }
            }
            index++;
        }
    }

    /// <summary>
    /// オフライン（単体テスト等）用にプレイヤーを生成・初期化する
    /// </summary>
    private void SpawnPlayerOffline()
    {
        int defaultIndex = 0;

        Transform spawnPoint = GetSpawnPoint(defaultIndex);
        PlayerRoot spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        GameManager.Instance.RegisterPlayer(spawnedPlayer);

        PlayerOfflineController controller = spawnedPlayer.gameObject.AddComponent<PlayerOfflineController>();
        //spawnedPlayer.Initialize(controller);

        Debug.Log("[PlayerSpawner] PlayerOfflineController をアタッチし、オフライン生成を完了しました。");
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