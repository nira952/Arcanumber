using Unity.Netcode;
using UnityEngine;

namespace nira.Demo
{
    /// <summary>
    /// NGOでのプレイヤー生成を行うクラス
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private Transform[] spawnPoints; // インデックス 0〜3 の初期位置
        [SerializeField] private DemoPlayer playerPrefab; // プレイヤーのプレハブ

        public override void OnNetworkSpawn()
        {
            // 自分がクライアントとして参加した時、サーバーへ生成をリクエストする
            if (IsClient)
            {
                int myIndex = PlayerDataManager.Instance.GetMyLobbyIndex();

                if (myIndex != -1 && PlayerDataManager.Instance.TryGetPlayerData(myIndex, out var myData))
                {
                    Debug.Log($"私はプレイヤー {myIndex}（名前: {myData.PlayerName}）です。生成をリクエストします。");
                    RequestSpawnServerRpc(myIndex);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestSpawnServerRpc(int index, ServerRpcParams rpcParams = default)
        {
            // ① サーバー側で指定のインデックスに応じた位置に生成
            Transform spawnPoint = spawnPoints[index % spawnPoints.Length];
            DemoPlayer spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // ② ネットワークオブジェクトとしてスポーンし、要求元のクライアントに所有権(Ownership)を渡す
            var networkObj = spawnedPlayer.GetComponent<NetworkObject>();
            networkObj.SpawnAsPlayerObject(rpcParams.Receive.SenderClientId);

            // ③ サーバー側でプレイヤーの初期化処理
            spawnedPlayer.Initialize(index);
        }
    }
}