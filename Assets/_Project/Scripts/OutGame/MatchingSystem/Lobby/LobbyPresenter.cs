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
        // LANモード切替のイベントを購読
        _view.OnLanModeToggled.Subscribe(isOn => _isLanMode = isOn).AddTo(_disposables);

        // オンラインマッチングのイベントを購読
        _view.OnJoinPrivateMatchRequested.Subscribe(_ =>
            (HandleMatchAsync(LobbyModel.MatchTypePrivate)).Forget()
        ).AddTo(_disposables);

        // カジュアルマッチングのイベントを購読
        _view.OnJoinCasualMatchRequested.Subscribe(_ =>
            ( HandleMatchAsync(LobbyModel.MatchTypeCasual)).Forget()
        ).AddTo(_disposables);


        // LANロビー作成のイベントを購読
        _view.OnLanHostRequested.Subscribe(_ =>
            (HandleLanMatchAsync(true)).Forget()
        ).AddTo(_disposables);

        // LAN参加のイベントを購読
        _view.OnLanJoinRequested.Subscribe(_ =>
            (HandleLanMatchAsync(false)).Forget()
        ).AddTo(_disposables);

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
            // 学校などでブロックされた場合ここに来る
            Debug.LogWarning($"[Presenter] UGS接続エラー(LANのみ可): {e.Message}");
            _view.UpdateLoadingMessage("オフラインモードです\n(LAN接続のみ使用可能)");
            await UniTask.Delay(2000, cancellationToken: _destroyToken);

            _view.HideLoading();
            _view.EnableJoinButton(); // エラーでもLAN用にボタンは解放する
        }
    }

    // ==========================================
    // 🌐 オンラインマッチ処理 (前回と同様)
    // ==========================================
    private async UniTask HandleMatchAsync(string matchType)
    {
        _isHost = false;
        _view.SetUIStateOnMatchingStart();
        _view.ShowLoading("マッチング中...");

        _matchCts?.Cancel();
        _matchCts = new CancellationTokenSource();
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_matchCts.Token, _destroyToken).Token;

        try
        {
            string targetLobbyName = $"Casual_{Guid.NewGuid():N}";

            if (matchType == LobbyModel.MatchTypePrivate)
            {
                string rawInput = _view.RoomCodeText.Trim();
                if (string.IsNullOrEmpty(rawInput)) { _view.ResetMatchUI(); return; }
                targetLobbyName = rawInput;

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
                // カジュアル処理 (前回コード略)
            }

            // ここからはオンライン・LAN共通の処理
            if (_isHost)
            {
                _networkModel.StartHostRelay(_myLocalPlayerId, PlayerDataManager.Instance.LocalPlayerName);
                _lobbyModel.HeartbeatLobbyAsync(linkedToken).Forget();
                SpawnPlayerDataManager();
            }
            else
            {
                await StartClientWaitAsync(_myLocalPlayerId, linkedToken); // ここは後述の StartClientWaitAsync で変更
            }

            _lobbyModel.StartLobbyPollingLoopAsync(linkedToken).Forget();
            _view.HideLoading();
            _view.ShowRoomPanel();
        }
        catch (Exception e) { HandleMatchError(e); }
    }

    // ==========================================
    // 🏫 LANマッチ処理 (新設)
    // ==========================================
    private async UniTask HandleLanMatchAsync(bool isStartingAsHost)
    {
        Debug.Log($"[Presenter] LANマッチ開始: {(isStartingAsHost ? "ホスト" : "クライアント")}モード");

        _isHost = isStartingAsHost;
        _view.SetUIStateOnMatchingStart();
        _view.ShowLoading("LAN接続中...");

        _matchCts?.Cancel();
        _matchCts = new CancellationTokenSource();
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_matchCts.Token, _destroyToken).Token;

        // HandleLanMatchAsync の一部
        try
        {
            if (_isHost)
            {
                string myIp = _networkModel.GetLocalIPAddress();
                _networkModel.StartHostLAN(_myLocalPlayerId, PlayerDataManager.Instance.LocalPlayerName, myIp);
                SpawnPlayerDataManager();

                _view.UpdateRoomName($"LANホスト: {myIp}");
                _view.HideLoading();
                _view.ShowRoomPanel();

                // ホスト用ループ
                LanPollingLoopAsync(linkedToken).Forget();
            }
            else
            {
                string targetIp = _view.TargetIPInputFieldText.Trim();
                if (string.IsNullOrEmpty(targetIp)) throw new Exception("IPアドレスを入力してください");

                // 💡 ここでホストへの接続完了を待つ
                await StartClientWaitAsync(_myLocalPlayerId, linkedToken, true, targetIp);

                _view.UpdateRoomName($"LAN参加中: {targetIp}");
                _view.HideLoading();
                _view.ShowRoomPanel();

                // 🛠️【追加】ゲスト用ループをここで起動する！
                LanClientPollingLoopAsync(linkedToken).Forget();
            }
        }
        catch (Exception e) { HandleMatchError(e); }
    }

    // LANホスト用：UGSロビーの代わりにNetcodeの接続数を直接監視する
    private async UniTask LanPollingLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _isHost && _isLanMode)
        {
            int connectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;

            // ホスト（自分）の名前
            _view.UpdatePlayerList(PlayerDataManager.Instance.LocalPlayerName + " (Host)", 1);

            if (connectedCount >= 2)
            {
                // ゲストの名前を辞書から探す
                string guestName = "接続中...";
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (clientId != NetworkManager.Singleton.LocalClientId && _networkModel.ClientIdToPlayerNameMap.TryGetValue(clientId, out var name))
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

    // 💡 新設：LANゲスト用：サーバー（ホスト）から同期されてくる接続状況を監視してUIに反映する
    private async UniTask LanClientPollingLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && !_isHost && _isLanMode)
        {
            // ネットワークが切断されていたらループを抜ける
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) break;

            // ゲスト視点では、NetworkManager.Singleton.ConnectedClientsIds には
            // 「自分」と「サーバー（ホスト）」のIDしか見えません。
            int connectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;

            // 1P枠：ホストの名前（まだ完全にデータを貰っていない間は「ホスト」と表示）
            _view.UpdatePlayerList("ホストプレイヤー", 1);

            // 2P枠：自分の名前（PlayerDataManagerから取得）
            _view.UpdatePlayerList(PlayerDataManager.Instance.LocalPlayerName + " (あなた)", 2);

            // 💡 もしより厳密にホストの名前をロビー表示中にも取得したい場合、
            // 今後の拡張としてNetworkVariableなどを使う必要がありますが、LAN内の2人プレイであればこの挙動で十分に「自分が参加できていること」が視覚的に確認できます。

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

        // 💡 ペイロードとして送る自分の名前を取得
        string myName = PlayerDataManager.Instance.LocalPlayerName;

        // 名前を引数に追加
        bool startResult = isLan ? _networkModel.StartClientLAN(playerId, myName, ip) : _networkModel.StartClientRelay(playerId, myName);
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
        Debug.LogError($"[Presenter] マッチングエラー: {e.Message}");
        _networkModel.Shutdown();
        _view.ResetMatchUI();
        _view.HideLoading();
    }

    private void UpdateLobbyUI(Lobby lobby)
    {
        // オンラインモード専用のUI更新処理 (前回コードと同様)
        if (_isLanMode) return;

        if (lobby == null)
        {
            HandleCancelOrLeaveAsync().Forget();
            return;
        }

        // --- 中略 (前回のUpdateLobbyUIの中身をそのまま配置) ---
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

    // ロビーデータに依存せず通信辞書からプレイヤーリストを構築する
    private async UniTask HandleStartGameAsync()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        var finalizedList = new List<PlayerNetworkData>();
        int index = 0;

        foreach (var kvp in _networkModel.PlayerIdToClientIdMap)
        {
            ulong clientId = kvp.Value;

            // 💡 辞書から名前を取得する（無ければPlayer〇という仮名にする）
            string pName = _networkModel.ClientIdToPlayerNameMap.TryGetValue(clientId, out var name) ? name : $"Player{index + 1}";

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