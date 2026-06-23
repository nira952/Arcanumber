using System.Threading;
using UnityEngine;

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
        if (uiManager == null) return;

        _lobbyModel = new LobbyModel();
        _networkSessionModel = new NetworkSessionModel();

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
        _presenter?.Dispose();
    }
}