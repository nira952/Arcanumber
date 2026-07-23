using Cysharp.Threading.Tasks;
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
        CloseAllPanels();
        SettingObservable();

        titlePanel.SetActive(true);
    }

    private void SettingObservable()
    {

        OnOpenPrivateMatchPanelRequested
            .Subscribe(_ =>
            {
                ChangeCurtainPanel(privateMatchPanel).Forget();
            })
            .AddTo(_disposables);

        // カジュアルマッチパネルを開くボタン処理
        OnOpenCasualMatchPanelRequested
            .Subscribe(_ =>
            {
                ChangeCurtainPanel(casualMatchPanel).Forget();

            })
            .AddTo(_disposables);

        OnOpenLanHostPanelRequested
            .Subscribe(_ =>
            {
                ChangeCurtainPanel(lanHostPanel).Forget();

            })
            .AddTo(_disposables);

        OnOpenLanJoinPanelRequested
            .Subscribe(_ =>
            {
                ChangeCurtainPanel(lanJoinPanel).Forget();

            })
            .AddTo(_disposables);

    }

    public async UniTask ChangeCurtainPanel(GameObject openPanel,GameObject closePanel = null)
    {
        // カーテンが閉じるまで待つ
        await CurtainManager.Instance.CloseAsync();

        // もし閉じるパネルが指定されていれば、それを非表示にする
        if (closePanel != null) { closePanel.SetActive(false); }

        // 全てのパネルを閉じる
        CloseAllPanels();

        openPanel.SetActive(true);

        CurtainManager.Instance.OpenAsync(GetType().Name).Forget();

    }


    private void CloseAllPanels()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (privateMatchPanel != null) privateMatchPanel.SetActive(false);
        if (casualMatchPanel != null) casualMatchPanel.SetActive(false);
        if (lanJoinPanel != null) lanJoinPanel.SetActive(false);
        if (lanHostPanel != null) lanHostPanel.SetActive(false);


    }

    public void OnClickExitTitlePanel(GameObject panel)
    {
        ChangeCurtainPanel(titlePanel, panel).Forget();
    }

}
