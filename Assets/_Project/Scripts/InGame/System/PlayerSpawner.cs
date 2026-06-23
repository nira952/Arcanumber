using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints; // インデックス 0〜3 の初期位置

    private void Start()
    {
        // ① 自分がこの部屋の「何番目のインデックスか」を取得
        int myIndex = PlayerDataManager.Instance.GetMyLobbyIndex();

        if (myIndex != -1 && PlayerDataManager.Instance.TryGetPlayerData(myIndex, out var myData))
        {
            Debug.Log($"私はプレイヤー {myIndex}（名前: {myData.PlayerName}）です。");

            // ② 自分のインデックスに応じた初期位置にキャラクターを配置
            Transform mySpawnPoint = spawnPoints[myIndex];

            // ③ 自分の色をキャラクターのレンダラーに適用
            // myCharacterRenderer.material.color = myData.SelectedColor;
        }
    }
}