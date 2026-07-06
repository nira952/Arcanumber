using System.Threading;
using UnityEngine;
using R3;

public class LobbyBootstrapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyUIManager uiManager;

    [Header("Settings")]
    [SerializeField] private string nextSceneName = "Game";

    private LobbyModel _lobbyModel;
    private NetworkSessionModel _networkSessionModel;

    // 💡 オンライン用とLAN用のPresenterを両方保持
    private LobbyPresenter _onlinePresenter; // ※ここからLANのロジックは消去してください
    private LanLobbyPresenter _lanPresenter;

    private bool _isLanMode = false; // 現在のモードを記録するフラグ

    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        if (uiManager == null) return;

        _lobbyModel = new LobbyModel();
        _networkSessionModel = new NetworkSessionModel();

        // 両方インスタンス化
        _onlinePresenter = new LobbyPresenter(uiManager, _lobbyModel, _networkSessionModel, nextSceneName, this.destroyCancellationToken);
        _lanPresenter = new LanLobbyPresenter(uiManager, _networkSessionModel, nextSceneName, this.destroyCancellationToken);

        // 💡 自身のローカルIPを表示する初期化処理などはBootstrapperで行うとスッキリします
        string myIp = _networkSessionModel.GetLocalIPAddress();
        uiManager.UpdateLocalIPText(myIp);

        // 💡 トグルの状態を記録する
        uiManager.OnLanModeToggled.Subscribe(isLanMode =>
        {
            _isLanMode = isLanMode; // モードを保存
            _networkSessionModel.Shutdown();
            uiManager.ResetMatchUI();
        }).AddTo(_disposables);

        SettingObservable();
    }
   
    private void SettingObservable()
    {
        // 💡 スタートボタン（シーン遷移）のイベントをここで一括管理する
        uiManager.OnNextSceneRequested.Subscribe(_ =>
        {
            if (_isLanMode)
            {
                _lanPresenter.StartGame();
            }
            else
            {
                _onlinePresenter.StartGame();
            }
        }).AddTo(_disposables);


        uiManager.OnCancelRequested.Subscribe(_ =>
        {
            if (_isLanMode)
            {
                _lanPresenter.HandleCancelOrLeave();
            }
            else
            {
                _onlinePresenter.HandleCancelOrLeave();
            }
        }).AddTo(_disposables);

    }


    private void OnDestroy()
    {
        _onlinePresenter?.Dispose();
        _lanPresenter?.Dispose();
        _disposables?.Dispose();
    }
}