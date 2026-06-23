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
    // UGS_ID または LAN用ローカルID と ClientId のマッピング
    public Dictionary<string, ulong> PlayerIdToClientIdMap { get; private set; } = new();

    public void Shutdown()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionApproval;
            NetworkManager.Singleton.Shutdown();
            PlayerIdToClientIdMap.Clear();
            Debug.Log("[NGO] NetworkManagerをシャットダウンしました。");
        }
    }

    // ==========================================
    // 🌐 オンライン (UGS Relay) モード
    // ==========================================
    public bool StartHostRelay(string hostPlayerId)
    {
        PlayerIdToClientIdMap.Clear();
        NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;

        bool isSuccess = NetworkManager.Singleton.StartHost();
        if (isSuccess) PlayerIdToClientIdMap[hostPlayerId] = NetworkManager.Singleton.LocalClientId;
        return isSuccess;
    }

    public bool StartClientRelay(string localPlayerId)
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(localPlayerId);
        return NetworkManager.Singleton.StartClient();
    }

    // ==========================================
    // 🏫 ローカル (LAN) モード
    // ==========================================
    public bool StartHostLAN(string hostPlayerId, string ipAddress, ushort port = 7777)
    {
        PlayerIdToClientIdMap.Clear();
        NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ipAddress, port); // 自IPをセット

        bool isSuccess = NetworkManager.Singleton.StartHost();
        if (isSuccess) PlayerIdToClientIdMap[hostPlayerId] = NetworkManager.Singleton.LocalClientId;
        return isSuccess;
    }

    public bool StartClientLAN(string localPlayerId, string targetIpAddress, ushort port = 7777)
    {
        // 1. 【特殊文字・空白の完全除去】
        // 見えない空白（ゼロ幅スペース \u200b）や、前後の余計なスペースを徹底的にクレンジングします
        string cleanIp = targetIpAddress
            .Replace("\u200b", "") // 💡 今回のエラーの原因である不可視文字を除去
            .Trim();               // 前後の半角・全角スペースを除去

        // 2. 【ポート番号の分離ロジック】
        // もし入力された文字列に「:」が含まれていた場合、IPとポートを正しく分離する
        if (cleanIp.Contains(":"))
        {
            string[] parts = cleanIp.Split(':');
            cleanIp = parts[0]; // 前半をIPとして上書き

            // もしコロンの後半にポート番号が書かれていたら、引数のポートではなくそちらを優先してパースする
            if (parts.Length > 1 && ushort.TryParse(parts[1], out ushort parsedPort))
            {
                port = parsedPort;
                Debug.Log($"[NGO] 入力文字列からポート番号を検出したため、ポートを {port} に変更しました。");
            }
        }

        // 3. 【★ご要望の確認ログ】
        // UnityTransportに渡す直前の「確定したIPとポート」を視覚的に分かりやすくログに出します
        Debug.Log($"<color=#00FF00>[NGO] ⬇︎接続設定の確認ログ⬇︎</color>\n" +
                  $"[確定IPアドレス] : \"{cleanIp}\"\n" +
                  $"[確定ポート番号] : {port}");

        // 4. Transportへの設定適用
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(cleanIp, port);
        }
        else
        {
            Debug.LogError("[NGO] UnityTransport コンポーネントが NetworkManager に見つかりません！");
            return false;
        }

        // 5. NGOクライアント起動
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(localPlayerId);
        bool result = NetworkManager.Singleton.StartClient();

        if (!result)
        {
            Debug.LogError($"[NGO] StartClient 自体の呼び出しに失敗しました。現在のTransport状態を確認してください。");
        }

        return result;
    }
    
    // ==========================================
    // 共通処理・ユーティリティ
    // ==========================================
    private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        try
        {
            string playerId = Encoding.UTF8.GetString(request.Payload);
            PlayerIdToClientIdMap[playerId] = request.ClientNetworkId;
            Debug.Log($"[NGO] 接続承認: ID({playerId}) -> ClientId({request.ClientNetworkId})");

            response.Approved = true;
            response.CreatePlayerObject = false;
        }
        catch (Exception)
        {
            response.Approved = false;
            response.Reason = "Invalid Payload";
        }
    }

    // 自分のLAN内IPアドレスを取得する
    public string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1"; // 取得失敗時のローカルループバック
    }
}