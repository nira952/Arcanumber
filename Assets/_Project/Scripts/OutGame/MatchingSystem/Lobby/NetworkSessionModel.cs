using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkSessionModel
{
    public Dictionary<string, ulong> PlayerIdToClientIdMap { get; private set; } = new();

    // 💡 新設: クライアントIDから「プレイヤー名」を引けるマップ
    public Dictionary<ulong, string> ClientIdToPlayerNameMap { get; private set; } = new();

    public void Shutdown()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionApproval;
            NetworkManager.Singleton.Shutdown();
            PlayerIdToClientIdMap.Clear();
            ClientIdToPlayerNameMap.Clear(); // 辞書をクリア
            Debug.Log("[NGO] NetworkManagerをシャットダウンしました。");
        }
    }

    // ==========================================
    // 🌐 オンライン (UGS Relay) モード
    // ==========================================
    public bool StartHostRelay(string hostPlayerId, string playerName)
    {
        PlayerIdToClientIdMap.Clear();
        ClientIdToPlayerNameMap.Clear();
        NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;

        bool isSuccess = NetworkManager.Singleton.StartHost();
        if (isSuccess)
        {
            PlayerIdToClientIdMap[hostPlayerId] = NetworkManager.Singleton.LocalClientId;
            ClientIdToPlayerNameMap[NetworkManager.Singleton.LocalClientId] = playerName; // ホスト自身の名前を登録
        }
        return isSuccess;
    }

    public bool StartClientRelay(string localPlayerId, string playerName)
    {
        SetConnectionPayload(localPlayerId, playerName);
        return NetworkManager.Singleton.StartClient();
    }

    // ==========================================
    // 🏫 ローカル (LAN) モード
    // ==========================================
    public bool StartHostLAN(string hostPlayerId, string playerName, ushort port = 7777)
    {
        PlayerIdToClientIdMap.Clear();
        ClientIdToPlayerNameMap.Clear();
        NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        // ホストは全インターフェース（0.0.0.0）で待ち受ける
        transport.SetConnectionData("0.0.0.0", port);

        bool isSuccess = NetworkManager.Singleton.StartHost();
        if (isSuccess)
        {
            string localIp = GetLocalIPAddress();
            Debug.Log($"[NGO] LANホストを開始しました。待受IP: 0.0.0.0 (公開IP: {localIp}), Port: {port}");

            PlayerIdToClientIdMap[hostPlayerId] = NetworkManager.Singleton.LocalClientId;
            ClientIdToPlayerNameMap[NetworkManager.Singleton.LocalClientId] = playerName;
        }
        return isSuccess;
    }
    public bool StartClientLAN(string localPlayerId, string playerName, string targetIpAddress, ushort port = 7777)
    {
        string cleanIp = targetIpAddress.Replace("\u200b", "").Trim();
        if (cleanIp.Contains(":"))
        {
            string[] parts = cleanIp.Split(':');
            cleanIp = parts[0];
            if (parts.Length > 1 && ushort.TryParse(parts[1], out ushort parsedPort)) port = parsedPort;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null) transport.SetConnectionData(cleanIp, port);

        SetConnectionPayload(localPlayerId, playerName);
        return NetworkManager.Singleton.StartClient();
    }

    // ==========================================
    // 共通処理・ユーティリティ
    // ==========================================

    // 💡 クライアントがホストに送るプロフィール情報（IDと名前を | で繋いで送る）
    private void SetConnectionPayload(string playerId, string playerName)
    {
        string payload = $"{playerId}|{playerName}";
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);
    }

    private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        try
        {
            CurtainManager.Instance.UpdateLoadingMessage("マッチング成功！");

            // 送られてきたPayloadを分解して、IDと名前に分ける
            string payloadStr = Encoding.UTF8.GetString(request.Payload);
            string[] parts = payloadStr.Split('|');

            string playerId = parts[0];
            string playerName = parts.Length > 1 ? parts[1] : "Guest";

            // 辞書に記憶する
            PlayerIdToClientIdMap[playerId] = request.ClientNetworkId;
            ClientIdToPlayerNameMap[request.ClientNetworkId] = playerName;

            Debug.Log($"[NGO] 接続承認: ID({playerId}) 名前({playerName}) -> ClientId({request.ClientNetworkId})");

            response.Approved = true;
            response.CreatePlayerObject = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[NGO] 接続承認エラー: {e.Message}");
            response.Approved = false;
            response.Reason = "Invalid Payload";
        }
    }

    public string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList) if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
        return "127.0.0.1";
    }
}