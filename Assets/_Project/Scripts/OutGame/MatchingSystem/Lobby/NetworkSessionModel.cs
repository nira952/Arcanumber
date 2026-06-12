using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Netcode for GameObjects (NGO) のセッション管理と、
/// 確実なClientIdマッピングを行うModel。
/// </summary>
public class NetworkSessionModel
{
    // UGSのPlayerIdをキー、NGOのClientIdを値とする確実なマッピング辞書
    public Dictionary<string, ulong> UgsIdToClientIdMap { get; private set; } = new();

    public void Shutdown()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionApproval;
            NetworkManager.Singleton.Shutdown();
            UgsIdToClientIdMap.Clear();
            Debug.Log("[NGO] NetworkManagerをシャットダウンしました。");
        }
    }

    public bool StartHost(string hostUgsPlayerId)
    {
        UgsIdToClientIdMap.Clear();

        // サーバー起動前にクライアントの接続承認コールバックを登録
        NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;

        // シーン管理の有効化（保険）
        if (NetworkManager.Singleton.NetworkConfig.EnableSceneManagement == false)
        {
            NetworkManager.Singleton.NetworkConfig.EnableSceneManagement = true;
        }

        bool isSuccess = NetworkManager.Singleton.StartHost();

        // ホスト自身もマッピングに登録しておく（ホストのClientIdは通常0）
        if (isSuccess)
        {
            UgsIdToClientIdMap[hostUgsPlayerId] = NetworkManager.Singleton.LocalClientId;
        }

        return isSuccess;
    }

    public bool StartClient(string localUgsPlayerId)
    {
        // クライアント接続時、自身のUGS PlayerIdをペイロード（ConnectionData）に仕込む
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(localUgsPlayerId);

        return NetworkManager.Singleton.StartClient();
    }

    /// <summary>
    /// クライアントからの接続要求を受け取った際のサーバー側（ホスト）の処理
    /// </summary>
    private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        try
        {
            // ペイロードからUGS PlayerIdを復元
            string ugsPlayerId = Encoding.UTF8.GetString(request.Payload);

            // 辞書に確実なマッピングを登録
            UgsIdToClientIdMap[ugsPlayerId] = request.ClientNetworkId;

            Debug.Log($"[NGO] 接続承認: UGS_ID({ugsPlayerId}) -> ClientId({request.ClientNetworkId})");

            response.Approved = true;
            response.CreatePlayerObject = false; // プロジェクトの仕様に合わせて変更してください
        }
        catch (Exception e)
        {
            Debug.LogError($"[NGO] 接続ペイロードの解析に失敗しました: {e.Message}");
            response.Approved = false;
            response.Reason = "Invalid Payload";
        }
    }
}