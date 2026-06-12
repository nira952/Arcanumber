using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

/// <summary>
/// MVPパターンのPresenter。
/// ViewとModelを仲介し、ゲームの進行（フロー）を管理する。
/// MonoBehaviourを継承しない純粋なC#クラス。
/// </summary>
public class LobbyPresenter : IDisposable
{
    private readonly LobbyUIManager _view;
    private readonly LobbyModel _lobbyModel;
    private readonly NetworkSessionModel _networkModel;
    private readonly string _nextSceneName;
    private readonly CancellationToken _destroyToken;

    private readonly CompositeDisposable _disposables = new();
    private CancellationTokenSource _matchCts;

    public LobbyPresenter(
        LobbyUIManager view,
        LobbyModel lobbyModel,
        NetworkSessionModel networkModel,
        string nextSceneName,
        CancellationToken destroyToken)
    {
        _view = view;
        _lobbyModel = lobbyModel;
        _networkModel = networkModel;
        _nextSceneName = nextSceneName;
        _destroyToken = destroyToken;

        _view.ResetMatchUI();
        BindEvents();

        // 起動時に自動で初期化
        InitializeServicesAsync().Forget();
    }

    private void BindEvents()
    {
        // 1. View（UI）からの入力イベントの購読
        _view.OnSignInRequested.Subscribe(_ => InitializeServicesAsync().Forget()).AddTo(_disposables);

        _view.OnJoinPrivateMatchRequested.Subscribe(_ => HandleMatchAsync(LobbyModel.MatchTypePrivate).Forget()).AddTo(_disposables);

        _view.OnJoinCasualMatchRequested.Subscribe(_ => HandleMatchAsync(LobbyModel.MatchTypeCasual).Forget()).AddTo(_disposables);

        _view.OnCancelRequested.Subscribe(_ => HandleCancelOrLeaveAsync().Forget()).AddTo(_disposables);

        _view.OnNextSceneRequested.Subscribe(_ => HandleStartGameAsync().Forget()).AddTo(_disposables);

        // 2. Modelからのデータ更新イベントの購読（UIへの反映）
        _lobbyModel.OnLobbyUpdated.Subscribe(lobby => UpdateLobbyUI(lobby)).AddTo(_disposables);
    }

    private async UniTask InitializeServicesAsync()
    {
        try
        {
            _view.ShowLoading("インターネットに接続中...");
            bool isConnected = await NetworkCheckUtility.CheckInternetConnectionAsync();

            if (!isConnected)
            {
                _view.UpdateLoadingMessage("インターネットに接続されていません。\n接続環境を確認してください。");
                _view.SetSignInButtonInteractable(true);
                return;
            }

            _view.ShowLoading("サービスに接続中...");
            await _lobbyModel.InitializeServicesAsync(_destroyToken);

            _view.HideLoading();
            _view.EnableJoinButton();
            _view.SetSignInButtonInteractable(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Presenter] 初期化エラー: {e.Message}");
        }
    }

    private async UniTask HandleMatchAsync(string matchType)
    {
        _view.SetUIStateOnMatchingStart();
        _view.ShowLoading("マッチング中...");

        _matchCts?.Cancel();
        _matchCts?.Dispose();
        _matchCts = new CancellationTokenSource();
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_matchCts.Token, _destroyToken).Token;

        try
        {
            string targetLobbyName = $"Casual_{Guid.NewGuid():N}";
            bool isHost = false;

            // --- プライベートマッチ ---
            if (matchType == LobbyModel.MatchTypePrivate)
            {
                string rawInput = _view.RoomCodeText.Trim();
                if (string.IsNullOrEmpty(rawInput))
                {
                    Debug.LogWarning("合言葉を入力してください。");
                    _view.ResetMatchUI();
                    return;
                }
                targetLobbyName = rawInput;

                _view.UpdateLoadingMessage("ロビー検索中...");
                var existingLobby = await _lobbyModel.FindAvailableLobbyByNameAsync(targetLobbyName, matchType);

                if (existingLobby != null)
                {
                    _view.UpdateLoadingMessage("ゲストとして参加中...");
                    await _lobbyModel.JoinLobbyAndRelayAsync(existingLobby, linkedToken);
                }
                else
                {
                    _view.UpdateLoadingMessage("ロビー作成中...");
                    await _lobbyModel.CreateLobbyAndRelayAsync(targetLobbyName, matchType, linkedToken);
                    isHost = true;
                }
            }
            // --- カジュアルマッチ ---
            else
            {
                var existingLobby = await HandleCasualMatchRetryLoopAsync(linkedToken);
                if (existingLobby != null)
                {
                    _view.UpdateLoadingMessage("ゲストとして参加中...");
                    await _lobbyModel.JoinLobbyAndRelayAsync(existingLobby, linkedToken);
                }
                else
                {
                    _view.UpdateLoadingMessage("ロビー作成中...");
                    await _lobbyModel.CreateLobbyAndRelayAsync(targetLobbyName, matchType, linkedToken);
                    isHost = true;
                }
            }

            // --- NGOの起動 ---
            string myUgsId = AuthenticationService.Instance.PlayerId;
            if (isHost)
            {
                _networkModel.StartHost(myUgsId);
                _lobbyModel.HeartbeatLobbyAsync(linkedToken).Forget();

                // PlayerDataManager の手動Spawn
                if (PlayerDataManager.Instance != null)
                {
                    var no = PlayerDataManager.Instance.GetComponent<NetworkObject>();
                    if (no != null && !no.IsSpawned) no.Spawn();
                }
            }
            else
            {
                await StartClientWaitAsync(myUgsId, linkedToken);
            }

            // 更新ループ開始
            _lobbyModel.StartLobbyPollingLoopAsync(linkedToken).Forget();

            _view.HideLoading();
            _view.ShowRoomPanel();
        }
        catch (OperationCanceledException)
        {
            _view.UpdateLoadingMessage("キャンセルしました");
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: _destroyToken);
            _view.HideLoading();
            _view.ResetMatchUI();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Presenter] マッチング重大エラー: {e.Message}");
            _networkModel.Shutdown();
            _view.ResetMatchUI();
        }
    }

    private async UniTask<Lobby> HandleCasualMatchRetryLoopAsync(CancellationToken token)
    {
        for (int i = 0; i < 3; i++)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                return await _lobbyModel.FindAvailableCasualLobbyAsync();
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogWarning($"[カジュアル] リトライ {i + 1}/3: {ex.Message}");
                await UniTask.Delay(UnityEngine.Random.Range(500, 1500), cancellationToken: token);
            }
        }
        return null; // リトライ失敗時は新規作成へ
    }

    private async UniTask StartClientWaitAsync(string ugsPlayerId, CancellationToken token)
    {
        var tcs = new UniTaskCompletionSource<bool>();
        void OnClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId) tcs.TrySetResult(true);
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        if (!_networkModel.StartClient(ugsPlayerId))
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            throw new Exception("StartClientに失敗。");
        }

        var timeout = UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: token);
        var winner = await UniTask.WhenAny(tcs.Task.AttachExternalCancellation(token), timeout);
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

        if (winner.Equals(1))
        {
            _networkModel.Shutdown();
            throw new Exception("サーバーへの接続がタイムアウトしました。");
        }
    }

    private void UpdateLobbyUI(Lobby lobby)
    {
        // ホスト解散等でロビーが消滅した場合
        if (lobby == null)
        {
            Debug.LogWarning("[Presenter] ロビーの消滅（または解散）を検知しました。");
            _networkModel.Shutdown();
            _view.ResetMatchUI();
            _view.HideRoomPanel();
            return;
        }

        bool isHost = lobby.HostId == AuthenticationService.Instance.PlayerId;

        // タイトル更新
        if (lobby.Data.TryGetValue(LobbyModel.MatchTypePrivate, out var typeData) && typeData.Value == LobbyModel.MatchTypePrivate)
            _view.UpdateRoomName(lobby.Name);
        else
            _view.UpdateRoomName("カジュアルマッチ");

        // プレイヤーリスト更新
        int index = 1;
        foreach (var player in lobby.Players)
        {
            if (player.Data != null && player.Data.TryGetValue("PlayerName", out var nameData))
            {
                string displayName = nameData.Value + (player.Id == lobby.HostId ? " (Host) " : "");
                _view.UpdatePlayerList(displayName, index);
            }
            index++;
        }

        _view.SetNextSceneButtonActive(isHost && lobby.Players.Count >= 2);
    }

    private async UniTask HandleCancelOrLeaveAsync()
    {
        _view.DisableCancelButton();
        _matchCts?.Cancel();

        await _lobbyModel.LeaveOrDeleteLobbyAsync();
        _networkModel.Shutdown();

        _view.ResetMatchUI();
        _view.HideRoomPanel();
        _view.HideLoading();
    }

    private async UniTask HandleStartGameAsync()
    {
        if (!NetworkManager.Singleton.IsServer || _lobbyModel.CurrentLobby == null) return;

        var ugsPlayers = _lobbyModel.CurrentLobby.Players;
        var finalizedList = new List<PlayerNetworkData>();

        for (int i = 0; i < ugsPlayers.Count; i++)
        {
            var ugsPlayer = ugsPlayers[i];
            string pName = ugsPlayer.Data.TryGetValue("PlayerName", out var nameData) ? nameData.Value : $"Player{i + 1}";

            // 💡 修正箇所: NetworkSessionModelの辞書から確実なClientIdを取得
            ulong matchedClientId = 0;
            if (_networkModel.UgsIdToClientIdMap.TryGetValue(ugsPlayer.Id, out ulong clientId))
            {
                matchedClientId = clientId;
            }
            else
            {
                Debug.LogWarning($"UGS ID {ugsPlayer.Id} に紐づくClientIdが見つかりません。");
            }

            finalizedList.Add(new PlayerNetworkData
            {
                LobbyIndex = i,
                ClientId = matchedClientId,
                PlayerName = pName,
            });
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