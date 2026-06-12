using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Networking;

public static class NetworkCheckUtility
{
    /// <summary>
    /// 実際に外部サーバーと通信し、インターネットに接続されているかを確認する
    /// </summary>
    public static async UniTask<bool> CheckInternetConnectionAsync()
    {
        // 1. 物理的な接続すら存在しない場合は即座に弾く
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("[NetworkCheck] 物理的なネットワーク接続がありません。");
            return false;
        }

        // 2. 実際に信頼できる外部サーバーに超軽量なリクエスト（HEAD）を送って確認する
        // ※Googleなどの大手ドメイン、またはご自身のゲームサーバーのURLを指定します
        string checkUrl = "https://www.google.com";

        try
        {
            using (UnityWebRequest request = UnityWebRequest.Head(checkUrl))
            {
                // タイムアウトを3秒に設定（これ以上待たせるとユーザーがイライラするため）
                request.timeout = 3;

                // 非同期で通信開始
                await request.SendWebRequest();

                // エラーがなく、ステータスコードが正常（200番台など）であれば接続OK
                if (request.result == UnityWebRequest.Result.Success)
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[NetworkCheck] 接続確認中に例外が発生しました: {ex.Message}");
        }

        Debug.LogWarning("[NetworkCheck] インターネットへの通信に失敗しました。");
        return false;
    }
}