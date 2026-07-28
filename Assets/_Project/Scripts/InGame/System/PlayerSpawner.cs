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

        // 自分がクライアントとして参加した時、サーバーへ自身の生成をリクエストする
        if (IsClient)
        {
            int myIndex = 0;

            if (PlayerDataManager.Instance != null)
            {
                myIndex = PlayerDataManager.Instance.GetMyLobbyIndex();
            }

            Debug.Log($"[PlayerSpawner] プレイヤー {myIndex} の生成をサーバーへリクエストします。");
            RequestSpawnServerRpc(myIndex);
        }
    }

    /// <summary>
    /// オフライン（単体テスト等）用にプレイヤーを生成・初期化する
    /// </summary>
    private void SpawnPlayerOffline()
    {
        int defaultIndex = 0;

        // 1. スポーン地点の決定（未設定時のエラー回避策付き）
        Transform spawnPoint = GetSpawnPoint(defaultIndex);

        // 2. プレハブの生成
        PlayerRoot spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        // 3. オフライン用コントローラーを動的にアタッチ
        PlayerOfflineController controller = spawnedPlayer.gameObject.AddComponent<PlayerOfflineController>();

        // 4. 初期化（PlayerOfflineController の Start 内で Initialize が実行されます）
        Debug.Log("[PlayerSpawner] PlayerOfflineController をアタッチし、オフライン生成を完了しました。");
    }

    /// <summary>
    /// クライアントからサーバーへプレイヤー生成を要請する (オンライン)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnServerRpc(int index, ServerRpcParams rpcParams = default)
    {
        if (isLocalMode) return;

        // 1. 指定インデックスのスポーン位置を取得
        Transform spawnPoint = GetSpawnPoint(index);

        // 2. サーバー側でプレイヤーを Instantiate
        PlayerRoot spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        // 3. オンライン用コントローラーを動的にアタッチ
        PlayerNetworkController controller = spawnedPlayer.gameObject.AddComponent<PlayerNetworkController>();

        // 4. NetworkObjectとしてスポーンさせ、要求元のクライアントへ所有権(Ownership)を譲渡する
        if (spawnedPlayer.TryGetComponent<NetworkObject>(out var networkObj))
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            networkObj.SpawnAsPlayerObject(senderClientId);
            Debug.Log($"[PlayerSpawner] ClientId: {senderClientId} 用の PlayerNetworkController をスポーンしました。");
        }
        else
        {
            Debug.LogError("[PlayerSpawner] プレイヤープレハブに NetworkObject コンポーネントが付与されていません！");
        }
    }

    /// <summary>
    /// インデックスに応じたスポーン地点を取得する（安全柵付き）
    /// </summary>
    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[Mathf.Abs(index) % spawnPoints.Length];
        }

        // スポーン位置が設定されていない場合はSpawner自身のTransformを返す
        return transform;
    }
}