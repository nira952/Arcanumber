using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public class LanLobbyPresenter : IDisposable
{
    private const int MinPlayersToStart = 2;

    private readonly LobbyUIManager _view;
    private readonly NetworkSessionModel _networkModel;
    private readonly string _nextSceneName;
    private readonly CancellationToken _destroyToken;

    private readonly CompositeDisposable _disposables = new();
    private CancellationTokenSource _matchCts;
    private bool _isHost = false;
    private string _myLocalPlayerId;

    public LanLobbyPresenter(
        LobbyUIManager view, NetworkSessionModel networkModel,
        string nextSceneName, CancellationToken destroyToken)
    {
        _view = view;
        _networkModel = networkModel;
        _nextSceneName = nextSceneName;
        _destroyToken = destroyToken;

        _myLocalPlayerId = Guid.NewGuid().ToString();
        BindEvents();
    }

    private void BindEvents()
    {
        _view.OnLanHostRequested.Subscribe(_ => HandleLanMatchAsync(true).Forget()).AddTo(_disposables);
        _view.OnLanJoinRequested.Subscribe(_ => HandleLanMatchAsync(false).Forget()).AddTo(_disposables);

        PlayerDataManager.Instance.AllPlayerData.OnListChanged += OnLanListChanged;
    }

    private void OnLanListChanged(NetworkListEvent<PlayerNetworkData> changeEvent)
    {
        // リストが変わったら、構造体対応版のUI更新を走らせる
        UpdateLanLobbyUI();
    }

    private async UniTask HandleLanMatchAsync(bool isStartingAsHost)
    {
        _isHost = isStartingAsHost;
        _view.SetUIStateOnMatchingStart();
        _view.ShowLoading("LAN接続中...");

        _matchCts?.Cancel();
        _matchCts = new CancellationTokenSource();
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_matchCts.Token, _destroyToken).Token;

        try
        {
            if (_isHost)
            {
                string myIp = _networkModel.GetLocalIPAddress();
                _networkModel.StartHostLAN(_myLocalPlayerId, PlayerDataManager.Instance.LocalPlayerName, myIp);
                SpawnPlayerDataManager();

                // 💡 参加・切断時フック（UI更新はOnListChanged側で行われるため、ここではデータの更新のみを行う）
                NetworkManager.Singleton.OnClientConnectedCallback += _ => {
                    PlayerDataManager.Instance.Server_UpdateLobbyData(_networkModel.ClientIdToPlayerNameMap);
                };
                NetworkManager.Singleton.OnClientDisconnectCallback += _ => {
                    PlayerDataManager.Instance.Server_UpdateLobbyData(_networkModel.ClientIdToPlayerNameMap);
                };

                PlayerDataManager.Instance.Server_UpdateLobbyData(_networkModel.ClientIdToPlayerNameMap);

                _view.UpdateRoomName($"IP : {myIp}");
                _view.HideLoading();
                _view.ShowRoomPanel();

                // 💡 競合の原因だった LanPollingLoopAsync の呼び出しを削除
            }
            else
            {
                string targetIp = _view.TargetIPInputFieldText.Trim();
                if (string.IsNullOrEmpty(targetIp)) throw new Exception("IPアドレスを入力してください");

                await StartClientWaitAsync(_myLocalPlayerId, linkedToken, true, targetIp);

                _view.UpdateRoomName($"IP : {targetIp}");
                _view.HideLoading();
                _view.ShowRoomPanel();

                // 💡 競合の原因だった LanClientPollingLoopAsync の呼び出しを削除

                // クライアント接続完了時に初期UI表示を反映
                UpdateLanLobbyUI();
            }
        }
        catch (Exception e) { HandleMatchError(e); }
    }

    // 💡 競合バグの元になっていた LanPollingLoopAsync と LanClientPollingLoopAsync は丸ごと削除しました。

    private void SpawnPlayerDataManager()
    {
        if (PlayerDataManager.Instance != null)
        {
            var no = PlayerDataManager.Instance.GetComponent<NetworkObject>();
            if (no != null && !no.IsSpawned) no.Spawn();
        }
    }

    private async UniTask StartClientWaitAsync(string playerId, CancellationToken token, bool isLan, string ip)
    {
        var tcs = new UniTaskCompletionSource<bool>();
        void OnConnected(ulong clientId) { if (clientId == NetworkManager.Singleton.LocalClientId) tcs.TrySetResult(true); }
        NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;

        string myName = PlayerDataManager.Instance.LocalPlayerName;
        bool startResult = _networkModel.StartClientLAN(playerId, myName, ip);

        if (!startResult)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
            throw new Exception("StartClientに失敗。");
        }

        var winner = await UniTask.WhenAny(tcs.Task.AttachExternalCancellation(token), UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: token));
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
        if (winner.Equals(1)) throw new Exception("サーバーへの接続がタイムアウトしました。");
    }

    private void HandleMatchError(Exception e)
    {
        _networkModel.Shutdown();
        _view.ResetMatchUI();
        _view.HideLoading();
    }

    public void HandleCancelOrLeave()
    {
        HandleCancelOrLeaveAsync().Forget();
    }

    private async UniTask HandleCancelOrLeaveAsync()
    {
        _view.DisableCancelButton();
        _matchCts?.Cancel();
        _networkModel.Shutdown();
        _view.ResetMatchUI();
        _view.HideRoomPanel();
        _view.HideLoading();
    }

    public void StartGame()
    {
        HandleStartGameAsync().Forget();
    }

    private async UniTask HandleStartGameAsync()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        var finalizedList = new List<PlayerNetworkData>();
        int index = 0;
        var processedClientIds = new HashSet<ulong>();

        foreach (var kvp in _networkModel.PlayerIdToClientIdMap)
        {
            ulong clientId = kvp.Value;
            if (processedClientIds.Contains(clientId)) continue;

            string pName = _networkModel.ClientIdToPlayerNameMap.TryGetValue(clientId, out var name) ? name : $"Player{index + 1}";
            finalizedList.Add(new PlayerNetworkData { LobbyIndex = index, ClientId = clientId, PlayerName = pName });
            processedClientIds.Add(clientId);
            index++;
        }

        PlayerDataManager.Instance.Server_BuildAndSyncPlayerData(finalizedList);
        _view.ShowLoading("シーンを移動します");
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: _destroyToken);
        _view.HideLoading();
        GameSceneManager.Instance.LoadNetworkScene(_nextSceneName);
    }

    /// <summary>
    /// LANモード時のUI更新ロジック（構造体対応版）
    /// </summary>
    private void UpdateLanLobbyUI()
    {
        if (NetworkManager.Singleton == null) return;

        ulong myClientId = NetworkManager.Singleton.LocalClientId;

        // 定員（MinPlayersToStart）の数だけループを回す
        for (int i = 0; i < MinPlayersToStart; i++)
        {
            PlayerNetworkData? targetPlayer = null;

            foreach (var data in PlayerDataManager.Instance.AllPlayerData)
            {
                Debug.Log($"[Presenter] スロット {i} をチェック中: {data.PlayerName.ToString()} (ClientId: {data.ClientId}, LobbyIndex: {data.LobbyIndex})");

                if (data.LobbyIndex == i)
                {
                    Debug.Log($"[Presenter] スロット {i} にプレイヤーが存在: {data.PlayerName.ToString()} (ClientId: {data.ClientId})");
                    targetPlayer = data;
                    break;
                }
            }

            if (targetPlayer.HasValue)
            {
                PlayerNetworkData playerData = targetPlayer.Value;
                string pName = playerData.PlayerName.ToString();

                // ホストかどうかの判定
                bool isTargetHost = playerData.ClientId == NetworkManager.ServerClientId || playerData.LobbyIndex == 0;

                // サフィックス（接尾辞）ルール
                string suffix = "";
                if (playerData.ClientId == myClientId)
                {
                    suffix = isTargetHost ? " (あなた/ホスト)" : " (あなた)";
                }
                else if (isTargetHost)
                {
                    suffix = " (ホスト)";
                }

                // UIのテキストを更新
                _view.UpdatePlayerList(pName + suffix, i + 1);
            }
            else
            {
                // データがないスロットは待機中表示
                _view.UpdatePlayerList("待機中...", i + 1);
            }
        }

        // ボタンの活性化チェック
        int ngoCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
        int registeredCount = PlayerDataManager.Instance.AllPlayerData.Count;

        _view.SetNextSceneButtonActive(_isHost && registeredCount >= MinPlayersToStart && ngoCount >= MinPlayersToStart);
    }

    public void Dispose()
    {
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.AllPlayerData != null)
        {
            PlayerDataManager.Instance.AllPlayerData.OnListChanged -= OnLanListChanged;
        }

        _matchCts?.Cancel();
        _matchCts?.Dispose();
        _disposables?.Dispose();
    }
}