using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    public const string PlayerNameKey = "PlayerName";

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject roomPanel;

    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TMP_InputField playerNameInput;

    [SerializeField] private Button lobbyJoinPrivateButton;
    [SerializeField] private Button lobbyJoinCasualButton;

    [SerializeField] private Button cancelOrLeaveButton;

    [SerializeField] private Button signInButton;
    [SerializeField] private Button offlineButton;

    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI[] playerListTexts = new TextMeshProUGUI[4];

    // 🟢 R3: ボタンのクリックイベントを外部（DemoManager）へ Observable として公開
    public Observable<Unit> OnJoinPrivateMatchRequested => lobbyJoinPrivateButton.OnClickAsObservable();

    public Observable<Unit> OnJoinCasualMatchRequested => lobbyJoinCasualButton.OnClickAsObservable();

    public Observable<Unit> OnCancelRequested => cancelOrLeaveButton.OnClickAsObservable();

    public Observable<Unit> OnSignInRequested => signInButton.OnClickAsObservable();

    public Observable<Unit> OnOfflineRequested => offlineButton.OnClickAsObservable();

    // 🟢 読み取り専用のプロパティ
    public string RoomCodeText => roomCodeInput != null ? roomCodeInput.text.Trim() : string.Empty;


    private void Start()
    {
        PlayerDataManager.Instance.LoadLocalPlayerData();

        // 初期UI状態のセット
        ResetMatchUI();

        playerNameInput.text = PlayerDataManager.Instance.LocalPlayerName;

        playerNameInput.onValueChanged.AddListener(_ => EnsurePlayerName());

        OnOfflineRequested.Subscribe(_ => HideLoading()).AddTo(this);
    }

    // playerNameInputが空白の場合、自動で[プレイヤー]に設定する（空白は許可しない）
    private void EnsurePlayerName()
    {
        if (playerNameInput != null && string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            playerNameInput.text = PlayerDataManager.DefaultPlayerNamePrefix;
        }

        PlayerDataManager.Instance.SaveLocalPlayerName(playerNameInput.text);
    }

    public void SetSignInButtonInteractable(bool interactable)
    {
        if (signInButton != null) signInButton.gameObject.SetActive(interactable);
        if (offlineButton != null) offlineButton.gameObject.SetActive(interactable);
    }


    /// <summary>
    /// 部屋パネルを表示する
    /// </summary>
    public void ShowRoomPanel()
    {
        if (roomPanel != null) roomPanel.SetActive(true);
    }

    /// <summary>
    /// 部屋パネルを非表示にする
    /// </summary>
    public void HideRoomPanel()
    {
        if (roomPanel != null) roomPanel.SetActive(false);
    }

    /// <summary>
    /// ローディングパネルを表示する
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    public void ShowLoading(string message)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (loadingText != null) loadingText.text = message;
    }

    /// <summary>
    /// ローディングメッセージを更新する
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    public void UpdateLoadingMessage(string message)
    {
        if (loadingText != null) loadingText.text = message;
    }

    /// <summary>
    /// ローディングパネルを非表示にする
    /// </summary>
    public void HideLoading()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }


    /// <summary>
    /// マッチング開始時のUI状態（連打防止）
    /// </summary>
    public void SetUIStateOnMatchingStart()
    {
        if (lobbyJoinPrivateButton != null) lobbyJoinPrivateButton.interactable = false;
        if (lobbyJoinCasualButton != null) lobbyJoinCasualButton.interactable = false;
        if (cancelOrLeaveButton != null) cancelOrLeaveButton.interactable = true;
    }

    /// <summary>
    /// UIを初期状態（待機中）に戻す
    /// </summary>
    public void ResetMatchUI()
    {
        if (lobbyJoinPrivateButton != null) lobbyJoinPrivateButton.interactable = true;
        if (lobbyJoinCasualButton != null) lobbyJoinCasualButton.interactable = true;
        if (cancelOrLeaveButton != null) cancelOrLeaveButton.interactable = false;

        if (roomNameText != null) roomNameText.text = "待機中...";
        if (playerListTexts != null)
        {
            foreach (var playerListText in playerListTexts)
            {
                if (playerListText != null) playerListText.text = string.Empty;
            }
        }
    }

    /// <summary>
    /// サインイン完了時にボタンを押せるようにする
    /// </summary>
    public void EnableJoinButton()
    {
        if (lobbyJoinPrivateButton != null) lobbyJoinPrivateButton.interactable = true;
        if (lobbyJoinCasualButton != null) lobbyJoinCasualButton.interactable = true;
    }

    /// <summary>
    /// キャンセルボタンの一時的な無効化（連打防止）
    /// </summary>
    public void DisableCancelButton()
    {
        if (cancelOrLeaveButton != null) cancelOrLeaveButton.interactable = false;
    }

    /// <summary>
    /// 部屋名テキストの更新
    /// </summary>
    public void UpdateRoomName(string text)
    {
        if (roomNameText != null) roomNameText.text = text;
    }

    /// <summary>
    /// 参加者リストテキストの更新
    /// </summary>
    public void UpdatePlayerList(string text, int index)
    {
        if (playerListTexts != null && index >= 0 && index < playerListTexts.Length)
        {
            if (playerListTexts[index] != null) playerListTexts[index].text = text;
        }
    }
}