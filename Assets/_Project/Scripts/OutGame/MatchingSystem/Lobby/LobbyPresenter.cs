using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyPresenter : IDisposable
{
    private readonly LobbyUIManager _view;
    private readonly LobbyModel _lobbyModel;
    private readonly NetworkSessionModel _networkModel;
    private readonly string _nextSceneName;
    private readonly CancellationToken _destroyToken;

    private readonly CompositeDisposable _disposables = new();
    private CancellationTokenSource _matchCts;

    // 現在のモード状態
    private bool _isLanMode = false;
    private bool _isHost = false;
    private string _myLocalPlayerId; // ネットワーク認証に依存しない固有ID

    public LobbyPresenter(
        LobbyUIManager view, LobbyModel lobbyModel, NetworkSessionModel networkModel,
        string nextSceneName, CancellationToken destroyToken)
    {
        _view = view;
        _lobbyModel = lobbyModel;
        _networkModel = networkModel;
        _nextSceneName = nextSceneName;
        _destroyToken = destroyToken;

        // オフラインでも一意に識別できるようにランダムGUIDを生成しておく
        _myLocalPlayerId = Guid.NewGuid().ToString();

        _view.ResetMatchUI();

        // 自身のローカルIPを取得してUIに表示（LANホスト用）
        string myIp = _networkModel.GetLocalIPAddress();
        _view.UpdateLocalIPText(myIp);

        BindEvents();
        InitializeServicesAsync().Forget();
    }

    private void BindEvents()
    {
        // ViewのトグルなどでLANモードが切り替わったときのイベント
        _view.OnLanModeToggled.Subscribe(isOn => _isLanMode = isOn).AddTo(_disposables);

        _view.OnJoinPrivateMatchRequested.Subscribe(_ =>
            (HandleMatchAsync(LobbyModel.MatchTypePrivate)).Forget()
        ).AddTo(_disposables);

        _view.OnJoinCasualMatchRequested.Subscribe(_ =>
            (HandleMatchAsync(LobbyModel.MatchTypeCasual)).Forget()
        ).AddTo(_disposables);

        _view.OnLanHostRequested.Subscribe(_ => HandleLanMatchAsync(true).Forget()).AddTo(_disposables);

        _view.OnLanJoinRequested.Subscribe(_ => HandleLanMatchAsync(false).Forget()).AddTo(_disposables);

        _view.OnCancelRequested.Subscribe(_ => HandleCancelOrLeaveAsync().Forget()).AddTo(_disposables);
        _view.OnNextSceneRequested.Subscribe(_ => HandleStartGameAsync().Forget()).AddTo(_disposables);

        _lobbyModel.OnLobbyUpdated.Subscribe(lobby => UpdateLobbyUI(lobby)).AddTo(_disposables);
    }

    private async UniTask InitializeServicesAsync()
    {
        try
        {
            _view.ShowLoading("サービスに接続中...");
            await _lobbyModel.InitializeServicesAsync(_destroyToken);

            // ログイン成功したらUGSのIDを優先使用
            _myLocalPlayerId = AuthenticationService.Instance.PlayerId;
            _view.HideLoading();
            _view.EnableJoinButton();
        }
        catch (Exception e)
        {
            // 学校などでブロックされた場合ここに来る（フェイルセーフ）
            Debug.LogWarning($"[Presenter] UGS接続エラー(LANのみ可): {e.Message}");
            _view.UpdateLoadingMessage("オフラインモードです\n(LAN接続のみ使用可能)");
            await UniTask.Delay(2000, cancellationToken: _destroyToken);

            _view.HideLoading();
            _view.EnableJoinButton(); // エラーでもLAN用にボタンは解放する
        }
    }

    // ==========================================
    // 🌐 オンラインマッチ処理 (UGS Relay)
    // ==========================================
    private async UniTask HandleMatchAsync(string matchType)
    {
        string targetLobbyName = "カジュアルマッチ";

        // プライベートマッチの場合は、ユーザーが入力した合言葉を使用する
        if (matchType == LobbyModel.MatchTypePrivate)
        {
            // プライベート合言葉ロジック
            string rawInput = _view.RoomCodeText.Trim();

            Debug.Log($"[Presenter] プライベートマッチの合言葉: {rawInput}");

            // 入力が空の場合
            if (string.IsNullOrEmpty(rawInput) || rawInput == " ")
            {
                Debug.Log("合言葉を入力してください");
                return;

            }

            targetLobbyName = rawInput;
        }

        _isHost = false;
        _view.SetUIStateOnMatchingStart();
        _view.ShowLoading("マッチング中...");

        _matchCts?.Cancel();
        _matchCts = new CancellationTokenSource();
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_matchCts.Token, _destroyToken).Token;

        try
        {

            if (matchType == LobbyModel.MatchTypePrivate)
            {

                var existingLobby = await _lobbyModel.FindAvailableLobbyByNameAsync(targetLobbyName, matchType);
                if (existingLobby != null)
                {
                    await _lobbyModel.JoinLobbyAndRelayAsync(existingLobby, linkedToken);
                }
                else
                {
                    await _lobbyModel.CreateLobbyAndRelayAsync(targetLobbyName, matchType, linkedToken);
                    _isHost = true;
                }
            }
            else
            {
                // カジュアルクイックジョインロジック
                // 1. 空いているカジュアルロビーがあるか検索する
                var casualLobby = await _lobbyModel.FindAvailableCasualLobbyAsync();

                if (casualLobby != null)
                {
                    // 2. 見つかった場合は、そのロビーとRelayサーバーに参加する
                    await _lobbyModel.JoinLobbyAndRelayAsync(casualLobby, linkedToken);
                }
                else
                {
                    // 3. 見つからなかった場合は、新しく自分でカジュアルロビーを作る（自分がホストになる）
                    await _lobbyModel.CreateLobbyAndRelayAsync(targetLobbyName, matchType, linkedToken);
                    _isHost = true;
                }
            }




            // NGO（Netcode）の起動
            if (_isHost)
            {
                // ホスト起動時に自分の名前を渡す
                _networkModel.StartHostRelay(_myLocalPlayerId, PlayerDataManager.Instance.LocalPlayerName);
                _lobbyModel.HeartbeatLobbyAsync(linkedToken).Forget();
                SpawnPlayerDataManager();
            }
            else
            {
                await StartClientWaitAsync(_myLocalPlayerId, linkedToken);
            }

            _lobbyModel.StartLobbyPollingLoopAsync(linkedToken).Forget();
            _view.HideLoading();
            _view.ShowRoomPanel();
        }
        catch (Exception e) { HandleMatchError(e); }
    }

    // ==========================================
    // 🏫 LANマッチ処理 (ローカルIP)
    // ==========================================
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

                // ホスト専用：クライアントが接続・切断したときにPlayerDataManagerのリストを更新・同期する
                NetworkManager.Singleton.OnClientConnectedCallback += _ => {
                    PlayerDataManager.Instance.Server_UpdateLobbyData(_networkModel.ClientIdToPlayerNameMap);
                };
                NetworkManager.Singleton.OnClientDisconnectCallback += _ => {
                    PlayerDataManager.Instance.Server_UpdateLobbyData(_networkModel.ClientIdToPlayerNameMap);
                };

                // 初回（自分自身だけがいる状態）のデータを一度同期
                PlayerDataManager.Instance.Server_UpdateLobbyData(_networkModel.ClientIdToPlayerNameMap);

                _view.UpdateRoomName($"IP : {myIp}");
                _view.HideLoading();
                _view.ShowRoomPanel();

                LanPollingLoopAsync(linkedToken).Forget();
            }
            else
            {
                string targetIp = _view.TargetIPInputFieldText.Trim();
                if (string.IsNullOrEmpty(targetIp)) throw new Exception("IPアドレスを入力してください");

                await StartClientWaitAsync(_myLocalPlayerId, linkedToken, true, targetIp);

                _view.UpdateRoomName($"IP : {targetIp}");
                _view.HideLoading();
                _view.ShowRoomPanel();

                // ゲスト側の画面更新ループを回す
                LanClientPollingLoopAsync(linkedToken).Forget();
            }
        }
        catch (Exception e) { HandleMatchError(e); }
    }

    // ==========================================
    // 待機画面の更新ループ (LAN専用)
    // ==========================================

    // 【ホスト用】NGOの接続数を監視し、送られてきた名前を表示する
    private async UniTask LanPollingLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _isHost && _isLanMode)
        {
            int connectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            _view.UpdatePlayerList(PlayerDataManager.Instance.LocalPlayerName + " (ホスト)", 1);

            if (connectedCount >= 2)
            {
                string guestName = "接続中...";
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (clientId != NetworkManager.Singleton.LocalClientId &&
                        _networkModel.ClientIdToPlayerNameMap.TryGetValue(clientId, out var name))
                    {
                        guestName = name;
                    }
                }

                _view.UpdatePlayerList(guestName, 2);
                _view.SetNextSceneButtonActive(true);
            }
            else
            {
                _view.UpdatePlayerList("待機中...", 2);
                _view.SetNextSceneButtonActive(false);
            }

            await UniTask.Delay(1000, cancellationToken: token);
        }
    }

    // 【ゲスト用】自分の参加状態を画面に反映させる
    private async UniTask LanClientPollingLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && !_isHost && _isLanMode)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) break;

            // 表示枠を一旦リセットして待機状態にする
            _view.UpdatePlayerList("待機中...", 1);
            _view.UpdatePlayerList("待機中...", 2);

            // 💡 ホストから同期されてきているNetworkList（AllPlayerData）をそのままUIに投影する
            foreach (var data in PlayerDataManager.Instance.AllPlayerData)
            {
                string suffix = "";
                if (data.ClientId == NetworkManager.Singleton.LocalClientId)
                {
                    suffix = " (あなた)";
                }
                else if (data.LobbyIndex == 0) // 通常インデックス0がホスト
                {
                    suffix = " (ホスト)";
                }

                // LobbyIndexは0始まりなので、UI用に+1する
                _view.UpdatePlayerList(data.PlayerName + suffix, data.LobbyIndex + 1);
            }

            await UniTask.Delay(1000, cancellationToken: token);
        }
    }
    // ==========================================
    // 共通処理群
    // ==========================================
    private void SpawnPlayerDataManager()
    {
        if (PlayerDataManager.Instance != null)
        {
            var no = PlayerDataManager.Instance.GetComponent<NetworkObject>();
            if (no != null && !no.IsSpawned) no.Spawn();
        }
    }

    private async UniTask StartClientWaitAsync(string playerId, CancellationToken token, bool isLan = false, string ip = "")
    {
        var tcs = new UniTaskCompletionSource<bool>();
        void OnConnected(ulong clientId) { if (clientId == NetworkManager.Singleton.LocalClientId) tcs.TrySetResult(true); }
        NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;

        string myName = PlayerDataManager.Instance.LocalPlayerName;
        bool startResult = isLan ? _networkModel.StartClientLAN(playerId, myName, ip) : _networkModel.StartClientRelay(playerId, myName);

        if (!startResult)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
            throw new Exception("StartClientに失敗。");
        }

        var winner = await UniTask.WhenAny(
            tcs.Task.AttachExternalCancellation(token),
            UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: token)
        );
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;

        if (winner.Equals(1)) throw new Exception("サーバーへの接続がタイムアウトしました。");
    }

    private void HandleMatchError(Exception e)
    {
        Debug.LogError($"[Presenter] マッチングエラー: {e.Message}");
        _networkModel.Shutdown();
        _view.ResetMatchUI();
        _view.HideLoading();
    }

    // オンライン(UGS)モード時のUI更新ロジック
    private void UpdateLobbyUI(Lobby lobby)
    {
        if (_isLanMode) return;

        if (lobby == null)
        {
            HandleCancelOrLeaveAsync().Forget();
            return;
        }

        _view.UpdateRoomName(lobby.Name);

        // 💡 自分のUGS IDを取得（「あなた」という表記をつけるかの判定用）
        string myPlayerId = AuthenticationService.Instance.PlayerId;

        for (int i = 0; i < 2; i++)
        {
            if (i < lobby.Players.Count)
            {
                var player = lobby.Players[i];

                // 💡 UGSに登録されているプレイヤーデータを引き出す
                string pName = "Unknown";
                if (player.Data != null && player.Data.TryGetValue("PlayerName", out var nameData))
                {
                    pName = nameData.Value; // ここで相手の本当の名前を取得！
                }

                // LANモードと同じように、直感的なサフィックス（接尾辞）を付ける
                string suffix = "";
                if (player.Id == myPlayerId)
                {
                    // 自分自身の場合
                    suffix = (player.Id == lobby.HostId) ? " (あなた/ホスト)" : " (あなた)";
                }
                else if (player.Id == lobby.HostId)
                {
                    // 自分ではないが、ホストの場合
                    suffix = " (ホスト)";
                }

                // UIのテキストを更新する
                _view.UpdatePlayerList(pName + suffix, i + 1);
            }
            else
            {
                _view.UpdatePlayerList("待機中...", i + 1);
            }
        }

        // 全員が揃ってNGO接続も完了したら「次へ」ボタンを有効化
        int ngoCount = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsIds.Count : 0;
        _view.SetNextSceneButtonActive(_isHost && lobby.Players.Count >= 2 && ngoCount >= 2);
    }


    private async UniTask HandleCancelOrLeaveAsync()
    {
        _view.DisableCancelButton();
        _matchCts?.Cancel();

        if (!_isLanMode) await _lobbyModel.LeaveOrDeleteLobbyAsync();
        _networkModel.Shutdown();

        _view.ResetMatchUI();
        _view.HideRoomPanel();
        _view.HideLoading();
    }

    // 💡 ゲームシーンへの遷移処理（オンライン・LAN共通）
    private async UniTask HandleStartGameAsync()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        var finalizedList = new List<PlayerNetworkData>();
        int index = 0;

        foreach (var kvp in _networkModel.PlayerIdToClientIdMap)
        {
            ulong clientId = kvp.Value;

            // マップから正しいプレイヤー名を取得する（万が一無い場合はフォールバック）
            string pName = _networkModel.ClientIdToPlayerNameMap.TryGetValue(clientId, out var name)
                ? name
                : $"Player{index + 1}";

            finalizedList.Add(new PlayerNetworkData
            {
                LobbyIndex = index,
                ClientId = clientId,
                PlayerName = pName,
            });
            index++;
        }

        PlayerDataManager.Instance.Server_BuildAndSyncPlayerData(finalizedList);

        _view.ShowLoading("シーンを移動します");
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: _destroyToken);
        _view.HideLoading();

        GameSceneManager.Instance.LoadNetworkScene(_nextSceneName);
    }

    public void Dispose()
    {
        _matchCts?.Cancel();
        _matchCts?.Dispose();
        _disposables?.Dispose();
    }
}