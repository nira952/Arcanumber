using R3;
using UnityEngine;
using UnityEngine.UI;


public class TitleUIManager : MonoBehaviour
{
    [SerializeField] private GameObject titlePanel;


    [SerializeField] private GameObject privateMatchPanel;

    [SerializeField] private GameObject casualMatchPanel;

    [SerializeField] private GameObject lanJoinPanel;

    [SerializeField] private GameObject lanHostPanel;

    [SerializeField] private Button privateMatchPanelButton;

    [SerializeField] private Button casualMatchPanelButton;

    [SerializeField] private Button lanJoinPanelButton;

  [SerializeField] private Button lanHostPanelButton;

    // --- ボタンのクリックイベントをObservableとして公開 ---

    public Observable<Unit> OnOpenPrivateMatchPanelRequested => privateMatchPanelButton.OnClickAsObservable();

    public Observable<Unit> OnOpenCasualMatchPanelRequested => casualMatchPanelButton.OnClickAsObservable();

    public Observable<Unit> OnOpenLanJoinPanelRequested => lanJoinPanelButton.OnClickAsObservable();

    public Observable<Unit> OnOpenLanHostPanelRequested => lanHostPanelButton.OnClickAsObservable();

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

        OnOpenLanHostPanelRequested
            .Subscribe(_ =>
            {
                CloseAllPanel();
                OpenLanHostPanel();
            })
            .AddTo(_disposables);

        OnOpenLanJoinPanelRequested
            .Subscribe(_ =>
            {
                CloseAllPanel();
                OpenLanJoinPanel();
            })
            .AddTo(_disposables);

    }

    public void CloseAllPanel()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (privateMatchPanel != null) privateMatchPanel.SetActive(false);
        if (casualMatchPanel != null) casualMatchPanel.SetActive(false);
        if (lanJoinPanel != null) lanJoinPanel.SetActive(false);
        if (lanHostPanel != null) lanHostPanel.SetActive(false);

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

    public void OpenLanJoinPanel() { if (lanJoinPanel != null) lanJoinPanel.SetActive(true); }


    public void OpenLanHostPanel() { if (lanHostPanel != null) lanHostPanel.SetActive(true); }


    public void ClosePanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(false);
    }

}
