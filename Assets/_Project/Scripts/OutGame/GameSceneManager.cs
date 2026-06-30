using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }
    private void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            // シーン遷移が開始されたときに走るログ（ホスト・ゲスト共通）
            NetworkManager.Singleton.SceneManager.OnLoad += (clientId, sceneName, loadSceneMode, asyncOperation) =>
            {
                Debug.Log($"[SceneLog] Client_{clientId} が {sceneName} への読み込みを開始しました。");
            };

            // シーン遷移が完全に完了したときに走るログ（ホスト・ゲスト共通）
            NetworkManager.Singleton.SceneManager.OnLoadComplete += (clientId, sceneName, loadSceneMode) =>
            {
                Debug.Log($"[SceneLog] Client_{clientId} の {sceneName} への遷移が完了しました！");
            };
        }
    }
    private void Awake()
    {
        // もし既にインスタンスが存在していれば、重複防止のために警告を出すか破棄する
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[GameSceneManager] 既にインスタンスが存在するため、重複したものを破棄します。");
            Destroy(this);
            return;
        }

        // 自分自身をインスタンスとして登録
        Instance = this;
    }

    /// <summary>
    /// ネットワーク同期を伴うシーン遷移（ホスト専用）
    /// </summary>
    public void LoadNetworkScene(string sceneName)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            // 現在接続しているすべてのゲストの確認
            int connectedClientsCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            Debug.Log($"[GameSceneManager] ホスト権限で同期シーン遷移を開始します。対象シーン: {sceneName} (接続クライアント数: {connectedClientsCount})");

            // NGOのSceneManagerから直接シーン遷移を実行する（第一引数にシーン名）
            SceneEventProgressStatus status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            // 遷移処理が正常に受け付けられたかログで確認する
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"[GameSceneManager] シーン遷移の開始に失敗しました。ステータス: {status}");
            }
        }
        else
        {
            Debug.LogWarning("[GameSceneManager] 自身がサーバー(ホスト)ではないため、ネットワーク遷移は無視されました。");
        }
    
    }

    /// <summary>
    /// 通常のローカルシーン遷移（タイトルに戻る場合や、オフライン画面用）
    /// </summary>
    public void LoadLocalScene(string sceneName)
    {
        Debug.Log($"[GameSceneManager] ローカル単体シーン遷移を開始します: {sceneName}");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}