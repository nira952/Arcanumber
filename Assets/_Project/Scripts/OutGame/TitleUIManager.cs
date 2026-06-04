using R3;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class TitleUIManager : MonoBehaviour
{
    [SerializeField] private GameObject titlePanel;

    [SerializeField] private GameObject nameInputPanel;

    [SerializeField] private GameObject privateMatchPanel;

    [SerializeField] private GameObject casualMatchPanel;

    [SerializeField] private GameObject lobbyPanel;

    [SerializeField] private GameObject loadingPanel;

    [SerializeField] private TextMeshProUGUI loadingStatusText;

    [SerializeField] private TextMeshProUGUI lobbyNameText;

    [SerializeField] private TMP_InputField nameInputField;

    [SerializeField] private TMP_InputField roomCodeInput;

    [SerializeField] private Button settingNameButton;

    [SerializeField] private Button nameResetButton;

    [SerializeField] private Button privateMatchPanelButton;

    [SerializeField] private Button casualMatchPanelButton;

    [SerializeField] private Button privateMatchButton;

    [SerializeField] private Button casualMatchButton;

    [SerializeField] private Transform playerCardParent;

    [SerializeField] private GameObject playerCardPrefab;


    public string NameInputText => nameInputField != null ? nameInputField.text : string.Empty;

    public string RoomCodeText => roomCodeInput != null ? roomCodeInput.text : string.Empty;

    // --- ボタンのクリックイベントをObservableとして公開 ---

    public Observable<Unit> OnSettingNameRequested => settingNameButton.OnClickAsObservable();

    public Observable<Unit> OnNameResetRequested => nameResetButton.OnClickAsObservable();

    public Observable<Unit> OnPrivateMatchRequested => privateMatchButton.OnClickAsObservable();

    public Observable<Unit> OnCasualMatchRequested => casualMatchButton.OnClickAsObservable();

    public Observable<Unit> OnOpenPrivateMatchPanelRequested => privateMatchPanelButton.OnClickAsObservable();

    public Observable<Unit> OnOpenCasualMatchPanelRequested => casualMatchPanelButton.OnClickAsObservable();



    private readonly CompositeDisposable _disposables = new();

    private void Awake()
    {
        // 最初はすべてのパネルを非表示にする
        CloseAllPanel();
        SettingObservable();
    }

    private void SettingObservable()
    {
        PlayerDataManager.Instance.OnPlayerListChanged
                    .Subscribe(_ =>
                    {
                        // リストに「追加」「更新」「削除」などの変化があったらここが走る
                        OnPlayerListUpdate();
                    })
                    .AddTo(_disposables);


        OnOpenPrivateMatchPanelRequested
            .Subscribe(_ =>
            {
                CloseAllPanel();
                OpenPrivateMatchPanel();
            })
            .AddTo(_disposables);

        OnOpenCasualMatchPanelRequested
            .Subscribe(_ =>
            {
                CloseAllPanel();
                OpenCasualMatchPanel();
            })
            .AddTo(_disposables);


        OnPrivateMatchRequested
            .Subscribe(_ =>
            {
                // ここでプライベートマッチの開始処理を呼び出す
                
            })
            .AddTo(_disposables);
    }

    public void MatchingManagerObservable(MatchingManager matchingManager)
    {

        matchingManager.CurrentLobbyName.Subscribe(lobbyName =>
        {
            if (string.IsNullOrEmpty(lobbyName))
            {
                // カジュアルマッチ
                SetLobbyNameText("- カジュアルマッチ -");
            }
            else
            {
                // プライベートマッチ
                SetLobbyNameText($"- {lobbyName} -");
            }
        })
        .AddTo(_disposables);
    }


    /// <summary>
    /// プレイヤーリストが更新されたときに呼ばれるメソッド
    /// </summary>
    public void OnPlayerListUpdate()
    {
        List<PlayerInfo> playerInfos
             = PlayerDataManager.Instance.GetAllPlayers();

        // 既存のプレイヤーカードをすべて削除
        foreach (Transform child in playerCardParent)
        {
            Destroy(child.gameObject);
        }

        // プレイヤーリストに基づいて新しいプレイヤーカードを生成
        foreach (PlayerInfo playerInfo in playerInfos)
        {
            GameObject playerCard = Instantiate(playerCardPrefab, playerCardParent);

            // プレイヤーカードの子オブジェクトからTextMeshProUGUIコンポーネントを取得して、プレイヤーの名前を表示
            TextMeshProUGUI nameText = playerCard.GetComponentInChildren<TextMeshProUGUI>();

            if (nameText != null)
            {
                nameText.text = playerInfo.Name.ToString();

            }
            else
            {
                Debug.LogWarning("プレイヤーカードにTextMeshProUGUIコンポーネントが見つかりませんでした。");
            }
        }
    }

    private void SetLobbyNameText(string lobbyName)
    {
        if (lobbyNameText != null)
        {
            lobbyNameText.text = lobbyName;
        }
    }

    public void CloseAllPanel()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(false);
        if (privateMatchPanel != null) privateMatchPanel.SetActive(false);
        if (casualMatchPanel != null) casualMatchPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
    }

    public void OpenTitlePanel() { titlePanel.SetActive(true); }

    public void OnClickExitTitlePanel(GameObject panel)
    {
        panel.SetActive(false);

        titlePanel.SetActive(true);
    }

    public void OpenNameInputPanel() { nameInputPanel.SetActive(true); }


    public void OpenPrivateMatchPanel() { privateMatchPanel.SetActive(true); }


    public void OpenCasualMatchPanel() { casualMatchPanel.SetActive(true); }

    public void OpenLobbyPanel() { lobbyPanel.SetActive(true); }

    public void OpenLoadingPanel(string loadingStatus) 
    { 
        loadingPanel.SetActive(true);

        loadingStatusText.text = loadingStatus;

    }

    public void UpdateLoadingStatus(string loadingStatus)
    {
        if (loadingStatusText != null)
        {
            loadingStatusText.text = loadingStatus;
        }
    }



}
