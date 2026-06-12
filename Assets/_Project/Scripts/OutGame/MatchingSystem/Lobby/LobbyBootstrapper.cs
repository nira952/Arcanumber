using System.Threading;
using UnityEngine;

/// <summary>
/// MVPパターンのエントリーポイント。
/// 各Modelを生成し、Viewと共にPresenterへ注入（Manual DI）する。
/// </summary>
public class LobbyBootstrapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyUIManager uiManager;

    [Header("Settings")]
    [SerializeField] private string nextSceneName = "Game";

    private LobbyModel _lobbyModel;
    private NetworkSessionModel _networkSessionModel;
    private LobbyPresenter _presenter;

    private void Start()
    {
        if (uiManager == null)
        {
            Debug.LogError("[Bootstrapper] UIManager がセットされていません。インスペクターを確認してください。");
            return;
        }

        // 1. Modelの生成（データ・通信層）
        _lobbyModel = new LobbyModel();
        _networkSessionModel = new NetworkSessionModel();

        // 2. Presenterへの依存関係の注入と起動（進行管理層）
        // MonoBehaviourのライフサイクルに紐づくCancellationTokenも渡す
        _presenter = new LobbyPresenter(
            uiManager,
            _lobbyModel,
            _networkSessionModel,
            nextSceneName,
            this.destroyCancellationToken
        );
    }

    private void OnDestroy()
    {
        // 破棄時にPresenterのイベント購読解除や通信の停止を呼び出す
        _presenter?.Dispose();
    }
}