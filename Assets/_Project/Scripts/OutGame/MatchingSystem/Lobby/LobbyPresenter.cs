using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;

/// <summary>
/// マッチングシステムのクラス
/// </summary>
public class LobbyPresenter : IDisposable
{
    private const int MinPlayersToStart = 2;    //最低限必要なプレイヤー数

    private readonly LobbyUIManager _view;
    private readonly LobbyModel _lobbyModel;
    private readonly NetworkSessionModel _networkModel;
    private readonly string _nextSceneName; //次のシーン名
    private readonly CancellationToken _destroyToken;   //破棄されるときのキャンセルトークン

    private readonly LobbyMatchCoordinator _matchCoordinator;
    private readonly LobbyPollingManager _pollingManager;

    private readonly CompositeDisposable _disposables = new();  //IDisposableをまとめて管理するためのCompositeDisposable
    private CancellationTokenSource _matchCts;  //マッチング処理用のキャンセルトークンソース

    private bool _isLanMode = false;    //LANモードかどうかのフラグ
    private bool _isHost = false;   //ホストかどうかのフラグ
    private string _myLocalPlayerId;    //自分のローカルID

    public LobbyPresenter(
        LobbyUIManager view, LobbyModel lobbyModel, NetworkSessionModel networkModel,
        string nextSceneName, CancellationToken destroyToken)
    {
        _view = view;
        _lobbyModel = lobbyModel;
        _networkModel = networkModel;
        _nextSceneName = nextSceneName;
        _destroyToken = destroyToken;

        _matchCoordinator = new LobbyMatchCoordinator(lobbyModel, networkModel);
        _pollingManager = new LobbyPollingManager();

        _myLocalPlayerId = Guid.NewGuid().ToString();

        _view.ResetMatchUI();
        _view.UpdateLocalIPText(_networkModel.GetLocalIPAddress());

        BindEvents();
        InitializeServicesAsync().Forget();
    }

    /// <summary>
    /// UIイベントのバインドを行う
    /// </summary>
    private void BindEvents()
    {
        _view.OnLanModeToggled.Subscribe(isOn => _isLanMode = isOn).AddTo(_disposables);

        _view.OnJoinPrivateMatchRequested.Subscribe(_ =>
            HandleMatchAsync(LobbyModel.MatchTypePrivate).Forget()
        ).AddTo(_disposables);

        _view.OnJoinCasualMatchRequested.Subscribe(_ =>
            HandleMatchAsync(LobbyModel.MatchTypeCasual).Forget()
        ).AddTo(_disposables);

        _lobbyModel.OnLobbyUpdated.Subscribe(lobby => UpdateLobbyUI(lobby)).AddTo(_disposables);
    }

    /// <summary>
    /// UGSサービスの初期化を非同期で行う
    /// </summary>
    private async UniTask InitializeServicesAsync()
    {
        try
        {
            CurtainManager.Instance.UpdateLoadingMessage("サービスに接続中...");
            await _lobbyModel.InitializeServicesAsync(_destroyToken);

            CurtainManager.Instance.UpdateLoadingMessage("接続完了！");
            _myLocalPlayerId = AuthenticationService.Instance.PlayerId;
            _view.EnableJoinButton();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Presenter] UGS接続エラー(LANのみ可): {e.Message}");
            CurtainManager.Instance.UpdateLoadingMessage("オフラインモードです\n(LAN接続のみ使用可能)");
            await UniTask.Delay(2000, cancellationToken: _destroyToken);

            _view.EnableJoinButton();
        }
    }

    /// <summary>
    /// マッチング処理を非同期で行う
    /// </summary>
    private async UniTask HandleMatchAsync(string matchType)
    {
        string targetLobbyName = "カジュアルマッチ";

        if (matchType == LobbyModel.MatchTypePrivate)
        {
            string rawInput = _view.RoomCodeText.Trim();
            if (string.IsNullOrEmpty(rawInput))
            {
                Debug.Log("合言葉を入力してください");
                return;
            }
            targetLobbyName = rawInput;
        }

        _isHost = false;
        await CurtainManager.Instance.CloseAsync("マッチング中...", GetType().Name);
        _view.SetUIStateOnMatchingStart();

        RenewMatchCancellationToken();

        try
        {
            // マッチメイク処理をCoordinatorに委譲
            _isHost = await _matchCoordinator.ProcessMatchMakingAsync(matchType, targetLobbyName, _myLocalPlayerId, _matchCts.Token);

            if (_isHost)
            {
                SetupHostNetworkSession();
                _pollingManager.RunPollingLoopAsync(
                    () => !_matchCts.Token.IsCancellationRequested && _isHost && !_isLanMode,
                    () => { if (_lobbyModel.CurrentLobby != null) UpdateLobbyUI(_lobbyModel.CurrentLobby); },
                    _matchCts.Token
                ).Forget();
            }
            else
            {
                await _matchCoordinator.StartClientWaitAsync(_myLocalPlayerId, _matchCts.Token);
                _pollingManager.RunPollingLoopAsync(
                    () => !_matchCts.Token.IsCancellationRequested && !_isHost && !_isLanMode && (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient),
                    () => { if (_lobbyModel.CurrentLobby != null) UpdateLobbyUI(_lobbyModel.CurrentLobby); },
                    _matchCts.Token
                ).Forget();
            }

            _view.ShowRoomPanel();
            CurtainManager.Instance.OpenAsync(GetType().Name).Forget();
        }
        catch (Exception e)
        {
            HandleMatchError(e);
        }
    }

    /// <summary>
    /// マッチングを安全に中断するためのメソッド
    /// </summary>
    private void RenewMatchCancellationToken()
    {
        _matchCts?.Cancel();
        _matchCts = CancellationTokenSource.CreateLinkedTokenSource(_destroyToken);
    }

    /// <summary>
    /// ホストとしてのネットワークセッションをセットアップする
    /// </summary>
    private void SetupHostNetworkSession()
    {
        _networkModel.StartHostRelay(_myLocalPlayerId, PlayerDataManager.Instance.LocalPlayerName);
        _lobbyModel.HeartbeatLobbyAsync(_matchCts.Token).Forget();
        SpawnPlayerDataManager();

        void UpdateData(ulong _) => PlayerDataManager.Instance.Server_UpdateLobbyData(_networkModel.ClientIdToPlayerNameMap);

        NetworkManager.Singleton.OnClientConnectedCallback += UpdateData;
        NetworkManager.Singleton.OnClientDisconnectCallback += UpdateData;

        PlayerDataManager.Instance.Server_UpdateLobbyData(_networkModel.ClientIdToPlayerNameMap);
    }

    /// <summary>
    /// シーン上に存在するPlayerDataManagerをスポーンさせる
    /// </summary>
    private void SpawnPlayerDataManager()
    {
        if (PlayerDataManager.Instance != null)
        {
            var no = PlayerDataManager.Instance.GetComponent<NetworkObject>();
            if (no != null && !no.IsSpawned) no.Spawn();
        }
    }

    /// <summary>
    /// マッチングエラーを処理する
    /// </summary>
    private void HandleMatchError(Exception e)
    {
        Debug.LogError($"[Presenter] マッチングエラー: {e.Message}");
        _networkModel.Shutdown();
        _view.ResetMatchUI();
        CurtainManager.Instance.OpenAsync(GetType().Name).Forget();
    }

    /// <summary>
    /// ロビーのUIを更新する
    /// </summary>
    private void UpdateLobbyUI(Lobby lobby)
    {
        if (_isLanMode) return;

        if (lobby == null)
        {
            HandleCancelOrLeaveAsync().Forget();
            return;
        }

        _view.UpdateRoomName(lobby.Name);
        string myPlayerId = AuthenticationService.Instance.PlayerId;

        for (int i = 0; i < MinPlayersToStart; i++)
        {
            if (i < lobby.Players.Count)
            {
                var player = lobby.Players[i];
                string pName = "Unknown";
                if (player.Data != null && player.Data.TryGetValue("PlayerName", out var nameData))
                    pName = nameData.Value;

                string suffix = "";
                if (player.Id == myPlayerId)
                    suffix = (player.Id == lobby.HostId) ? " (あなた/ホスト)" : " (あなた)";
                else if (player.Id == lobby.HostId)
                    suffix = " (ホスト)";

                _view.UpdatePlayerList(pName + suffix, i + 1);
            }
            else
                _view.UpdatePlayerList("待機中...", i + 1);
        }

        int ngoCount = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsIds.Count : 0;
        bool canStartGame = _isHost && lobby.Players.Count >= MinPlayersToStart && ngoCount >= MinPlayersToStart;

        _view.SetNextSceneButtonActive(canStartGame);
    }

    //キャンセル処理
    public void HandleCancelOrLeave() => HandleCancelOrLeaveAsync().Forget();

    /// <summary>
    /// 退出またはキャンセル処理を非同期で行う
    /// </summary>
    private async UniTask HandleCancelOrLeaveAsync()
    {
        CurtainManager.Instance.UpdateLoadingMessage("キャンセル中...");

        _view.DisableCancelButton();
        _matchCts?.Cancel();

        if (!_isLanMode) await _lobbyModel.LeaveOrDeleteLobbyAsync();
        _networkModel.Shutdown();

        _view.ResetMatchUI();
    }

    /// ゲーム開始処理
    public void StartGame() => HandleStartGameAsync().Forget();

    /// <summary>
    /// 次のシーンに移動するためのゲーム開始処理を行う
    /// </summary>
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

            string pName = _networkModel.ClientIdToPlayerNameMap.TryGetValue(clientId, out var name)
                ? name
                : $"Player{index + 1}";

            finalizedList.Add(new PlayerNetworkData
            {
                LobbyIndex = index,
                ClientId = clientId,
                PlayerName = pName,
            });

            processedClientIds.Add(clientId);
            index++;
        }

        PlayerDataManager.Instance.Server_BuildAndSyncPlayerData(finalizedList);

        await UniTask.Delay(TimeSpan.FromSeconds(0.3f), cancellationToken: _destroyToken);
        await CurtainManager.Instance.CloseAsync("シーンを移動します", GetType().Name);
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: _destroyToken);

        GameSceneManager.Instance.LoadNetworkScene(_nextSceneName);
        CurtainManager.Instance.OpenAsync(GetType().Name).Forget();
    }

    /// <summary>
    /// 破棄されるときに呼び出されるメソッド
    /// </summary>
    public void Dispose()
    {
        _matchCts?.Cancel();
        _matchCts?.Dispose();
        _disposables?.Dispose();
    }
}