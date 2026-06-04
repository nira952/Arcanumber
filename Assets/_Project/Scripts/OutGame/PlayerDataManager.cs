using R3;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerDataManager : NetworkBehaviour
{
    // --- シングルトンインスタンス ---
    public static PlayerDataManager Instance { get; private set; }


    // --- プレイヤー情報管理 ---

    // 自分の全プレイヤー情報
    public PlayerSettingData myPlayerData;

    // ネットワーク上で全プレイヤーの情報を管理するリスト
    private readonly NetworkList<PlayerInfo> _networkPlayerInfo = new();


    // --- コールバックイベント ---

    private readonly Subject<NetworkListEvent<PlayerInfo>> _onPlayerListChanged = new();
    public Observable<NetworkListEvent<PlayerInfo>> OnPlayerListChanged => _onPlayerListChanged;


    // プレイヤーの情報を設定する
    public void SetPlayerData(PlayerSettingData data)
    {
        myPlayerData = data;
    }


    // --- 情報取得関数 ---

    /// <summary>
    /// プレイヤーの情報を取得する
    /// </summary>
    /// <returns></returns>
    public PlayerSettingData GetPlayerData()
    {
        return myPlayerData;
    }


    public int GetPlayerIndex(ulong clientId)
    {
        for (int i = 0; i < _networkPlayerInfo.Count; i++)
        {
            if (_networkPlayerInfo[i].Id.ToString() == clientId.ToString())
            {
                return i;
            }
        }
        return -1; // 見つからない場合は-1を返す
    }



    // --- DataManagerの基本的な処理 ---

    /// <summary>
    /// シングルトンインスタンスの初期化
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        UpdatePlayerIndex();
    }

    // プレイヤーリストが更新されたとき、PlayerInfoのIndexにリストのIndexをセットする
    private void UpdatePlayerIndex()
    {
        OnPlayerListChanged.Subscribe(changeEvent =>
        {
            if (changeEvent.Type == NetworkListEvent<PlayerInfo>.EventType.Add || changeEvent.Type == NetworkListEvent<PlayerInfo>.EventType.Value)
            {
                int index = changeEvent.Index;
                PlayerInfo info = _networkPlayerInfo[index];
                info.LobbyIndex = index;
                _networkPlayerInfo[index] = info; // 更新したPlayerInfoをリストに戻す
            }
        }).AddTo(this);

    }


    // ロビーに参加した際に呼ばれる
    public override void OnNetworkSpawn()
    {
        _networkPlayerInfo.OnListChanged += OnListChangedHandler;

        if (IsServer)
        {
            // サーバー側でクライアントの切断を監視するためのコールバックを登録する
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        Debug.Log($"[Client] ネットワークに接続しました。クライアントID: {NetworkManager.Singleton.LocalClientId}, 名前: {myPlayerData.playerInfo}");
        // ネットワークが繋がったので、サーバーに名前を登録する
        RegisterPlayerNameServerRpc(myPlayerData.playerInfo);
    }

    /// <summary>
    /// ネットワークが切断された瞬間に自動で呼ばれる
    /// </summary>
    public override void OnNetworkDespawn()
    {
        _networkPlayerInfo.OnListChanged -= OnListChangedHandler;
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }


    /// <summary>
    /// ネットワークリストが変更されたときに呼ばれるハンドラー
    /// </summary>
    /// <param name="changeEvent"></param>
    private void OnListChangedHandler(NetworkListEvent<PlayerInfo> changeEvent)
    {
        _onPlayerListChanged.OnNext(changeEvent);
    }

    [Rpc(SendTo.Server)]
    private void RegisterPlayerNameServerRpc(PlayerInfo playerName, RpcParams rpcParams = default)
    {
        // クライアントから送られてきた名前を、サーバー側のリストに登録する
        ulong clientId = rpcParams.Receive.SenderClientId;
        PlayerInfo fixedName = playerName;
        int index = (int)clientId;

        while (_networkPlayerInfo.Count <= index)
        {
            _networkPlayerInfo.Add(new PlayerInfo());
        }

        _networkPlayerInfo[index] = fixedName;
        Debug.Log($"[Server] プレイヤー {clientId} の名前を '{playerName}' として登録しました。");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if ((int)clientId < _networkPlayerInfo.Count)
        {
            _networkPlayerInfo[(int)clientId] = new PlayerInfo();
        }
    }

    public List<PlayerInfo> GetAllPlayers()
    {
        var list = new List<PlayerInfo>();

        foreach (var player in _networkPlayerInfo)
        {
            // 空のデータ（切断済みなど）を除外したい場合はここで弾く
            list.Add(player);
        }
        return list;
    }
}