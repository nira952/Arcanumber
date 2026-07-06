using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
[RequireComponent(typeof(NetworkObject))]
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
    // 💡 引数に NetworkVariableReadPermission.Everyone を指定する
    private readonly NetworkList<PlayerNetworkData> _allPlayerData =
        new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    public NetworkList<PlayerNetworkData> AllPlayerData => _allPlayerData;

    [SerializeField] private List<string> previewAllPlayerNames = new List<string>();


    // --- ローカルスキル ----

    [SerializeField] private Skill[] mySkills = new Skill[4]; // スキル

    [SerializeField] private Arcana myArcana;   // アルカナ


    private void Awake()
    {
        // もし既にインスタンスが存在していれば、重複防止のために警告を出すか破棄する
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PlayerDataManager] 既にインスタンスが存在するため、重複したものを破棄します。");
            Destroy(this);
            return;
        }

        // 自分自身をインスタンスとして登録
        Instance = this;
    }

    private void Start()
    {
        // ホスト（サーバー）が起動したタイミングで自動的にネットワークに登録する
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += () =>
            {
                var no = this.gameObject.GetComponent<NetworkObject>();
                if (no != null && !no.IsSpawned)
                {
                    no.Spawn();
                    Debug.Log("[PlayerDataManager] サーバー起動を検知し、自動Spawnしました。");
                }
            };
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

    /// <summary>
    /// 自分のスキルを取得する
    /// </summary>
    /// <returns></returns>
    public Skill[] GetMySkills()
    {
        return mySkills;
    }

    /// <summary>
    /// 自分のアルカナを取得する
    /// </summary>
    /// <returns></returns>
    public Arcana GetMyArcana()
    {
        return myArcana;
    }


    /// <summary>
    /// ロビー内の総人数を取得する
    /// </summary>
    public int GetLobbyPlayerCount()
    {
        Debug.Log($"[PlayerDataManager] 現在のロビー人数: {_allPlayerData.Count}");

        return _allPlayerData.Count;
    }

    // ==========================================
    // 【サーバー専用】データをセットする処理
    // ==========================================

    /// <summary>
    /// 【ホスト専用】ゲーム開始直前に、確定したロビーの情報をまとめてセットする
    /// </summary>
    public void Server_BuildAndSyncPlayerData(List<PlayerNetworkData> finalizedList)
    {
        if (!IsServer) return;

        _allPlayerData.Clear();

        // 重複チェック用のハッシュセットを用意
        var addedClientIds = new HashSet<ulong>();

        foreach (var data in finalizedList)
        {
            // すでに同じClientIdが追加されている場合はスキップ（2重登録を防止）
            if (addedClientIds.Contains(data.ClientId))
            {
                Debug.LogWarning($"[PlayerDataManager] 重複したClientIdを発見したためスキップしました: {data.ClientId} (Name: {data.PlayerName})");
                continue;
            }

            _allPlayerData.Add(data);
            addedClientIds.Add(data.ClientId); // 追加済みリストに登録
        }

        // デバッグ用に、現在の全プレイヤー名をリストに格納しておく
        previewAllPlayerNames.Clear();
        foreach (var data in _allPlayerData)
        {
            previewAllPlayerNames.Add(data.PlayerName.ToString());
        }

        Debug.Log($"[PlayerDataManager] {_allPlayerData.Count}人分のプレイヤーデータを確定・同期しました。");
    }


    /// <summary>
    /// 【ホスト専用】ロビー待機中、接続人数が変わるたびに現在のメンバー名リストを全クライアントへ即時同期する
    /// </summary>
    public void Server_UpdateLobbyData(Dictionary<ulong, string> clientIdToNameMap)
    {
        if (!IsServer) return;

        _allPlayerData.Clear();
        int index = 0;

        // 現在接続されている全ClientIdをループ
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            // 名前マップから取得（なければフォールバック）
            string pName = clientIdToNameMap.TryGetValue(clientId, out var name) ? name : $"Player{index + 1}";

            _allPlayerData.Add(new PlayerNetworkData
            {
                LobbyIndex = index,
                ClientId = clientId,
                PlayerName = pName
            });
            index++;
        }

        // デバッグ用に、現在の全プレイヤー名をリストに格納しておく
        previewAllPlayerNames.Clear();
        foreach (var data in _allPlayerData)
        {
            previewAllPlayerNames.Add(data.PlayerName.ToString());
        }

        Debug.Log($"[PlayerDataManager] ロビーの同期データを更新しました。現在人数: {_allPlayerData.Count}人");
    }

    // ==========================================
    // 【サーバー専用】クライアントから送られてきたスキル番号をNetworkListに適用する
    // ==========================================

    /// <summary>
    /// 【ホスト専用】指定されたClientIdのプレイヤーデータを探し、
    /// スキル情報を上書きして全クライアントへ同期する
    /// </summary>
    public void Server_UpdatePlayerSkills(ulong clientId, int[] selectedSkillNos)
    {
        if (!IsServer) return;

        // 全体のNetworkList内から、送ってきたClientIdと一致するインデックスを探す
        for (int i = 0; i < _allPlayerData.Count; i++)
        {
            if (_allPlayerData[i].ClientId == clientId)
            {
                // 構造体は値型なので、一旦ローカルにコピーして取り出す
                PlayerNetworkData data = _allPlayerData[i];

                // 先ほどの int[] から、引数で渡された Skill[] の疑似データを作って構造体にセットする
                // （PlayerNetworkData.SetSkill が型として Skill[] を要求しているため、疑似オブジェクトの配列を作ります）
                Skill[] tempSkillArray = new Skill[selectedSkillNos.Length];
                for (int j = 0; j < selectedSkillNos.Length; j++)
                {
                    // 空のSkillを作成し、内部のスキル番号だけを一致させる
                    Skill dummySkill = ScriptableObject.CreateInstance<Skill>();
                }

                data.SetSkillFromIds(selectedSkillNos);

                // NetworkListの内容を上書き更新し全プレイヤーに自動同期
                _allPlayerData[i] = data;

                Debug.Log($"[Server] ClientId: {clientId} のスキルデータを構造体に書き込み、同期しました。");
                break;
            }
        }
    }

    // ==========================================
    // 【サーバー専用】クライアントから送られてきたアルカナデータをNetworkListに適用する
    // ==========================================

    /// <summary>
    /// 【ホスト専用】指定されたClientIdのプレイヤーデータを探し、アルカナ情報を上書きして全クライアントへ同期する
    /// </summary>
    public void Server_UpdatePlayerArcana(ulong clientId, ArcanaList selectedArcana)
    {
        if (!IsServer) return;

        // 全体のNetworkList内から、送ってきたClientIdと一致するインデックスを探す
        for (int i = 0; i < _allPlayerData.Count; i++)
        {
            if (_allPlayerData[i].ClientId == clientId)
            {
                // 構造体を取り出す（値型なのでコピーされる）
                PlayerNetworkData data = _allPlayerData[i];

                // アルカナ情報を上書き
                data.arcana = selectedArcana;

                // NetworkListの内容を上書き更新（これで全プレイヤーに自動同期される）
                _allPlayerData[i] = data;

                Debug.Log($"[Server] ClientId: {clientId} のアルカナデータを【{selectedArcana}】に更新し、同期しました。");
                break;
            }
        }
    }

    // ==========================================
    // 【サーバー専用】クライアントの準備状態を更新する
    // ==========================================

    /// <summary>
    /// 【ホスト専用】指定されたClientIdの準備完了状態を上書きする
    /// </summary>
    public void Server_SetPlayerReady(ulong clientId, bool isReady)
    {
        if (!IsServer) return;

        for (int i = 0; i < _allPlayerData.Count; i++)
        {
            if (_allPlayerData[i].ClientId == clientId)
            {
                PlayerNetworkData data = _allPlayerData[i];
                data.IsReady = isReady;
                _allPlayerData[i] = data; // NetworkListを更新（全クライアントへ即時同期）

                Debug.Log($"[Server] ClientId: {clientId} の準備状態を {isReady} に更新しました。");
                break;
            }
        }
    }

    // ==========================================
    // ✍️ 【ローカル専用】データをセットするメソッド群
    // ==========================================


    /// <summary>
    /// ローカルスキルを設定する
    /// </summary>
    /// <param name="skills"></param>
    public void SetLocalSkills(Skill[] skills)
    {
        if (skills == null || skills.Length != 5)
        {
            Debug.LogError("[PlayerDataManager] スキル配列の長さが不正です。5つのスキルを設定してください。");
            return;
        }

        mySkills = skills;
    }

    /// <summary>
    /// ローカルアルカナを設定する
    /// </summary>
    /// <param name="arcana"></param>
    public void SetLocalArcana(Arcana arcana)
    {
        myArcana = arcana;
    }


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
