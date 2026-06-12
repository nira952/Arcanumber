using R3;
using UnityEngine;
using UnityEngine.UI;


public class TitleUIManager : MonoBehaviour
{
    [SerializeField] private GameObject titlePanel;


    [SerializeField] private GameObject privateMatchPanel;

    [SerializeField] private GameObject casualMatchPanel;

    [SerializeField] private Button privateMatchPanelButton;

    [SerializeField] private Button casualMatchPanelButton;

    // --- ボタンのクリックイベントをObservableとして公開 ---

    public Observable<Unit> OnOpenPrivateMatchPanelRequested => privateMatchPanelButton.OnClickAsObservable();

    public Observable<Unit> OnOpenCasualMatchPanelRequested => casualMatchPanelButton.OnClickAsObservable();


    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        // 最初はすべてのパネルを非表示にする
        CloseAllPanel();
        SettingObservable();

        OpenTitlePanel();
    }

    private void SettingObservable()
    {

        // 名前設定ボタンがクリックされたときの処理
        OnOpenPrivateMatchPanelRequested
            .Subscribe(_ =>
            {
                CloseAllPanel();
                OpenPrivateMatchPanel();
            })
            .AddTo(_disposables);

        // カジュアルマッチパネルを開くボタン処理
        OnOpenCasualMatchPanelRequested
            .Subscribe(_ =>
            {
                CloseAllPanel();
                OpenCasualMatchPanel();
            })
            .AddTo(_disposables);

        // プライベートマッチパネルを開くボタン処理
        OnOpenPrivateMatchPanelRequested
            .Subscribe(_ =>
            {
                CloseAllPanel();
                OpenPrivateMatchPanel();
            })
            .AddTo(_disposables);

    }

    public void CloseAllPanel()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (privateMatchPanel != null) privateMatchPanel.SetActive(false);
        if (casualMatchPanel != null) casualMatchPanel.SetActive(false);
    }

    public void OpenTitlePanel() 
    { 
        CloseAllPanel();

        if (titlePanel != null) 
            titlePanel.SetActive(true); 
    }

    public void OnClickExitTitlePanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(false);
        if (titlePanel != null) titlePanel.SetActive(true);
    }

    public void OpenPrivateMatchPanel() { if (privateMatchPanel != null) privateMatchPanel.SetActive(true); }

    public void OpenCasualMatchPanel() { if (casualMatchPanel != null) casualMatchPanel.SetActive(true); }

}
