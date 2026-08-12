using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// マッチメイクの調整を行うクラス
/// </summary>
public class LobbyMatchCoordinator
{
    private readonly LobbyModel _lobbyModel;
    private readonly NetworkSessionModel _networkModel;

    public LobbyMatchCoordinator(LobbyModel lobbyModel, NetworkSessionModel networkModel)
    {
        _lobbyModel = lobbyModel;
        _networkModel = networkModel;
    }

    /// <summary>
    /// 部屋の作成または参加を行う。ホストかどうかを返す
    /// </summary>
    public async UniTask<bool> ProcessMatchMakingAsync(string matchType, string targetLobbyName, string myPlayerId, CancellationToken token)
    {
        bool isHost = false;

        if (matchType == LobbyModel.MatchTypePrivate)
        {
            var existingLobby = await _lobbyModel.FindAvailableLobbyByNameAsync(targetLobbyName, matchType);
            if (existingLobby != null)
                await _lobbyModel.JoinLobbyAndRelayAsync(existingLobby, token);
            else
            {
                await _lobbyModel.CreateLobbyAndRelayAsync(targetLobbyName, matchType, token);
                isHost = true;
            }
        }
        else
        {
            var casualLobby = await _lobbyModel.FindAvailableCasualLobbyAsync();
            if (casualLobby != null)
                await _lobbyModel.JoinLobbyAndRelayAsync(casualLobby, token);
            else
            {
                await _lobbyModel.CreateLobbyAndRelayAsync(targetLobbyName, matchType, token);
                isHost = true;
            }
        }

        _lobbyModel.StartLobbyPollingLoopAsync(token).Forget();
        return isHost;
    }

    /// <summary>
    /// クライアントの接続を待機する
    /// </summary>
    public async UniTask StartClientWaitAsync(string playerId, CancellationToken token, bool isLan = false, string ip = "")
    {
        var tcs = new UniTaskCompletionSource<bool>();
        void OnConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
                tcs.TrySetResult(true);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;

        try
        {
            string myName = PlayerDataManager.Instance.LocalPlayerName;
            bool startResult = isLan ? _networkModel.StartClientLAN(playerId, myName, ip) : _networkModel.StartClientRelay(playerId, myName);

            if (!startResult)
                throw new Exception("StartClientに失敗。");

            var (hasConnected, _) = await UniTask.WhenAny(
                tcs.Task.AttachExternalCancellation(token),
                UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: token)
            );

            if (!hasConnected)
                throw new Exception("サーバーへの接続がタイムアウトしました。");
        }
        catch (Exception)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
                NetworkManager.Singleton.Shutdown();
            throw;
        }
        finally
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
        }
    }
}