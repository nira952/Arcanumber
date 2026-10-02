using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class TitleUIManager : MonoBehaviour
{
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private CanvasGroup mainPanelGroup;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI titleSubText;

    [SerializeField] private TextMeshProUGUI guideText;


    [SerializeField] private GameObject privateMatchPanel;

    [SerializeField] private GameObject casualMatchPanel;

    [SerializeField] private GameObject lanJoinPanel;

    [SerializeField] private GameObject lanHostPanel;


    [SerializeField] private Button titlePanelButton;
    [SerializeField] private Button privateMatchPanelButton;

    [SerializeField] private Button casualMatchPanelButton;

    [SerializeField] private Button lanJoinPanelButton;

    [SerializeField] private Button lanHostPanelButton;

    [SerializeField] private Button trainingSceneButton;

    // --- ボタンのクリックイベントをObservableとして公開 ---


    public Observable<Unit> OnOpenTitlePanelRequested => titlePanelButton.OnClickAsObservable();
    public Observable<Unit> OnOpenPrivateMatchPanelRequested => privateMatchPanelButton.OnClickAsObservable();

    public Observable<Unit> OnOpenCasualMatchPanelRequested => casualMatchPanelButton.OnClickAsObservable();

    public Observable<Unit> OnOpenLanJoinPanelRequested => lanJoinPanelButton.OnClickAsObservable();

    public Observable<Unit> OnOpenLanHostPanelRequested => lanHostPanelButton.OnClickAsObservable();

    public Observable<Unit> OnOpenTrainingSceneRequested => trainingSceneButton.OnClickAsObservable();

    private readonly CompositeDisposable _disposables = new();

    private Tween guideTween;

    private void Start()
    {
        // 最初はすべてのパネルを非表示にする
        CloseAllPanels();
        SettingObservable();

        mainPanel.SetActive(true);
        titlePanel.SetActive(true);

        // DOTweenでguideTextを点滅させる
        guideTween = guideText.DOFade(0f, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        mainPanelGroup.alpha = 0f; // 最初は透明にする
        mainPanelGroup.interactable = false; // 最初は操作不可にする
    }

    private void SettingObservable()
    {
        OnOpenTitlePanelRequested
            .Subscribe(_ =>
            {
                StartTitle().Forget();
            })
            .AddTo(_disposables);

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

    public async UniTask StartTitle()
    {
        // TitlePanelButtonを非表示に
        titlePanelButton.gameObject.SetActive(false);

        float fadeDuration = 1f; // フェードアウトの時間

        float moveY = 400f; // 上に移動する距離

        float scale = 0.8f; // 小さくするスケール

        // TitleTextをDotweenでアニメーションさせる
        // 上部に移動し、scaleを小さくする
        titleText.transform.DOLocalMoveY(moveY, fadeDuration).SetEase(Ease.InOutQuad);

        titleText.transform.DOScale(scale, fadeDuration).SetEase(Ease.InOutQuad);

        // TitleSubTextをDotweenでフェードアウト
        titleSubText.DOFade(0f, fadeDuration).SetEase(Ease.InOutQuad);

        // guideTextのフェードアウトを止める
        guideTween.Kill();

        guideText.DOFade(0f, fadeDuration).SetEase(Ease.InOutQuad);

        // mainPanelを表示する
        mainPanelGroup.DOFade(1f, fadeDuration).SetEase(Ease.InOutQuad);

        mainPanelGroup.interactable = true;

        await UniTask.Delay((int)fadeDuration * 1000); // ミリ秒に変換

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



    public void CloseAllPanels()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (privateMatchPanel != null) privateMatchPanel.SetActive(false);
        if (casualMatchPanel != null) casualMatchPanel.SetActive(false);
        if (lanJoinPanel != null) lanJoinPanel.SetActive(false);
        if (lanHostPanel != null) lanHostPanel.SetActive(false);


    }

    public void OnClickExitTitlePanel(GameObject panel)
    {
        ChangeCurtainPanel(mainPanel, panel).Forget();
    }

}
