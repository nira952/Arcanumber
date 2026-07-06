using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// UGS (Lobby, Relay, Auth) のAPI通信とデータ保持を担当するModel。
/// </summary>
public class LobbyModel
{
    public const string MatchTypePrivate = "Private";
    public const string MatchTypeCasual = "Casual";
    private const string RelayKey = "RelayJoinCode";
    private const string MatchTypeKey = "MatchType";
    private const int MaxPlayers = 4;

    public Lobby CurrentLobby { get; private set; }

    // ロビー情報が更新されたことをPresenterに通知するためのSubject (R3)
    private readonly Subject<Lobby> _onLobbyUpdated = new();
    public Observable<Lobby> OnLobbyUpdated => _onLobbyUpdated;

    public async UniTask InitializeServicesAsync(CancellationToken cancellationToken)
    {
        InitializationOptions options = new InitializationOptions();
#if !UNITY_EDITOR
        options.SetProfile("Player_BuildClient");
#else
        options.SetProfile("Player_Editor");
#endif
        await UnityServices.InitializeAsync(options);

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[Services] サインイン完了: {AuthenticationService.Instance.PlayerId}");
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
        }
    }

    public async UniTask<Lobby> FindAvailableLobbyByNameAsync(string lobbyName, string matchType)
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 1,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.Name, lobbyName, QueryFilter.OpOptions.EQ),
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    new QueryFilter(QueryFilter.FieldOptions.S1, matchType, QueryFilter.OpOptions.EQ)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            return response.Results.Count > 0 ? response.Results[0] : null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"[Lobby API] ロビー検索失敗: {e.Message}");
            return null;
        }
    }

    public async UniTask<Lobby> FindAvailableCasualLobbyAsync()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 1,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    new QueryFilter(QueryFilter.FieldOptions.S1, MatchTypeCasual, QueryFilter.OpOptions.EQ)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            return response.Results.Count > 0 ? response.Results[0] : null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"[Lobby API] カジュアル検索失敗: {e.Message}");
            return null;
        }
    }

    public async UniTask CreateLobbyAndRelayAsync(string lobbyName, string matchType, CancellationToken cancellationToken)
    {
        Debug.Log($"[Debug] ロビー作成要求 - 名前(合言葉): {lobbyName}, タイプ: {matchType}");

        cancellationToken.ThrowIfCancellationRequested();
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
        string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            true
        );

        string playerName = PlayerDataManager.Instance.LocalPlayerName;
        if (string.IsNullOrEmpty(playerName) || playerName == PlayerDataManager.DefaultPlayerNamePrefix)
        {
            playerName = "Player1";
        }

        var player = new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>
        {
            { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
        });

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Player = player,
            Data = new Dictionary<string, DataObject>
            {
                { RelayKey, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) },
                { MatchTypeKey, new DataObject(DataObject.VisibilityOptions.Public, matchType, DataObject.IndexOptions.S1) }
            }
        };

        cancellationToken.ThrowIfCancellationRequested();
        CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MaxPlayers, options);
        _onLobbyUpdated.OnNext(CurrentLobby);
    }

    public async UniTask JoinLobbyAndRelayAsync(Lobby targetLobby, CancellationToken cancellationToken)
    {
        string playerName = PlayerDataManager.Instance.LocalPlayerName;
        if (string.IsNullOrEmpty(playerName) || playerName == PlayerDataManager.DefaultPlayerNamePrefix)
        {
            playerName = $"Player{targetLobby.Players.Count + 1}";
        }

        JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions
        {
            Player = new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            })
        };

        cancellationToken.ThrowIfCancellationRequested();
        CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(targetLobby.Id, joinOptions);

        if (!CurrentLobby.Data.TryGetValue(RelayKey, out var relayData))
            throw new Exception("ロビーデータ内にRelayコードが見つかりません。");

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayData.Value);

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData,
            true
        );

        _onLobbyUpdated.OnNext(CurrentLobby);
    }

    public async UniTask HeartbeatLobbyAsync(CancellationToken cancellationToken)
    {
        string lobbyId = CurrentLobby?.Id;
        while (CurrentLobby != null && CurrentLobby.Id == lobbyId && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            }
            catch (LobbyServiceException ex) when (ex.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                break;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Lobby] ハートビート送信失敗: {e.Message}");
            }
            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(15), cancellationToken: cancellationToken).SuppressCancellationThrow();
            if (isCanceled) break;
        }
    }

    public async UniTask StartLobbyPollingLoopAsync(CancellationToken cancellationToken)
    {
        string lobbyId = CurrentLobby?.Id;
        while (CurrentLobby != null && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                CurrentLobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
                _onLobbyUpdated.OnNext(CurrentLobby); // Presenterへ更新を通知
            }
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                CurrentLobby = null;
                _onLobbyUpdated.OnNext(null); // 解散通知
                break;
            }
            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(1.1f), cancellationToken: cancellationToken).SuppressCancellationThrow();
            if (isCanceled) break;
        }
    }

    public async UniTask LeaveOrDeleteLobbyAsync()
    {
        if (CurrentLobby == null) return;

        try
        {
            bool isHost = CurrentLobby.HostId == AuthenticationService.Instance.PlayerId;
            if (isHost)
            {
                await LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id);
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Lobby API] 退出処理中にエラー: {e.Message}");
        }
        finally
        {
            CurrentLobby = null;
        }
    }
}