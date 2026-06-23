using Unity.Netcode;
using UnityEngine;

public class PlayerDataManager : NetworkBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // セーブデータ用のキー
    private const string NameSaveKey = "Save_PlayerName";

    // 初期名の定数
    public const string DefaultPlayerNamePrefix = "プレイヤー";

    // ネットワーク接続前に、自分の名前や設定を一時保存しておくローカル変数
    public string LocalPlayerName { get; private set; } = DefaultPlayerNamePrefix;


    // ホストがデータを確定させ、全クライアントへ自動同期するリスト
    private readonly NetworkList<PlayerNetworkData> _allPlayerData = new();
    public NetworkList<PlayerNetworkData> AllPlayerData => _allPlayerData;




    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==========================================
    // プレイヤーがデータを取得するための便利なメソッド群
    // ==========================================

    /// <summary>
    /// 自分のローカルClientIdから、自分のインデックスを調べる
    /// </summary>
    public int GetMyLobbyIndex()
    {
        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        foreach (var data in _allPlayerData)
        {
            if (data.ClientId == myClientId) return data.LobbyIndex;
        }
        return -1; // 見つからない場合
    }

    /// <summary>
    /// 引数として渡されたインデックス（0〜3など）から、該当プレイヤーの全データを取得する
    /// </summary>
    public bool TryGetPlayerData(int index, out PlayerNetworkData resultData)
    {
        foreach (var data in _allPlayerData)
        {
            if (data.LobbyIndex == index)
            {
                resultData = data;
                return true;
            }
        }
        resultData = default;
        return false;
    }

    // ==========================================
    // 【サーバー専用】データをセットする処理
    // ==========================================

    /// <summary>
    /// 【ホスト専用】ゲーム開始直前に、確定したロビーの情報をまとめてセットする
    /// </summary>
    public void Server_BuildAndSyncPlayerData(System.Collections.Generic.List<PlayerNetworkData> finalizedList)
    {
        if (!IsServer) return;

        _allPlayerData.Clear();
        foreach (var data in finalizedList)
        {
            _allPlayerData.Add(data);
        }

        Debug.Log($"[PlayerDataManager] {_allPlayerData.Count}人分のプレイヤーデータを確定・同期しました。");
    }

    // ==========================================
    // ✍️ 【ローカル専用】名前をセットする処理
    // ==========================================

    /// <summary>
    /// ネットワーク接続前：ローカルに保存されている名前をロードする
    /// </summary>
    public void LoadLocalPlayerData()
    {
        if (PlayerPrefs.HasKey(NameSaveKey))
        {
            LocalPlayerName = PlayerPrefs.GetString(NameSaveKey);
            Debug.Log($"[PlayerDataManager] 前回の名前をロードしました: {LocalPlayerName}");
        }
        else
        {
            LocalPlayerName = DefaultPlayerNamePrefix;
            Debug.Log($"[PlayerDataManager] セーブデータがないため初期名を生成: {LocalPlayerName}");
        }
    }

    /// <summary>
    /// 🟢 タイトル画面の入力欄などで名前が変更されたときに呼び出し、ローカルに保存する
    /// </summary>
    public void SaveLocalPlayerName(string newName)
    {
        if (string.IsNullOrEmpty(newName)) return;

        LocalPlayerName = newName;
        PlayerPrefs.SetString(NameSaveKey, newName);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerDataManager] 新しい名前をローカルに保存しました: {newName}");
    }
}
