using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    [SerializeField] private GameObject titlePanel;

    [SerializeField] private GameObject nameInputPanel;

    [SerializeField] private GameObject privateRoomPanel;

    [SerializeField] private GameObject lobbyPanel;

    [SerializeField] private GameObject loadingPanel;

    [SerializeField] private TextMeshProUGUI loadingStatusText;

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

    public Observable<Unit> OnSettingNameRequested => settingNameButton.OnClickAsObservable();

    public Observable<Unit> OnNameResetRequested => nameResetButton.OnClickAsObservable();

    public Observable<Unit> OnPrivateMatchRequested => privateMatchButton.OnClickAsObservable();

    public Observable<Unit> OnCasualMatchRequested => casualMatchButton.OnClickAsObservable();


    private void Awake()
    {
        // 最初はすべてのパネルを非表示にする
        CloseAllPanel();
    }

    private void CloseAllPanel()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(false);
        if (privateRoomPanel != null) privateRoomPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    public void OpenTitlePanel() { titlePanel.SetActive(true); }

    public void CloseTitlePanel() { titlePanel.SetActive(false); }

    public void OnClickExitTitlePanel(GameObject panel)
    {
        panel.SetActive(false);

        titlePanel.SetActive(true);
    }

    public void OpenNameInputPanel() { nameInputPanel.SetActive(true); }

    public void CloseNameInputPanel() { nameInputPanel.SetActive(false); }

    public void OpenPrivateRoomPanel() { privateRoomPanel.SetActive(true); }

    public void ClosePrivateRoomPanel() { privateRoomPanel.SetActive(false); }

    public void OpenLobbyPanel() { lobbyPanel.SetActive(true); }

    public void CloseLobbyPanel() { lobbyPanel.SetActive(false); }

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

    public void CloseLoadingPanel() { loadingPanel.SetActive(false); }


}
