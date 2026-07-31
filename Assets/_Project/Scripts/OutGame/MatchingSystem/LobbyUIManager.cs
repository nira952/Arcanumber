using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MVPパターンのView。
/// UIコンポーネントの参照を保持し、ユーザーの入力をPresenterへ通知（Subject）する。
/// ロジックやネットワーク処理は一切持たない純粋なクラス。
/// </summary>
public class LobbyUIManager : MonoBehaviour
{
    [Header("--- パネル ---")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject roomPanel;

    [Header("--- メインメニュー UI ---")]
    [SerializeField] private Button joinCasualButton;
    [SerializeField] private Button joinPrivateButton;
    [SerializeField] private TMP_InputField roomCodeInput;    // UGS用合言葉

    [SerializeField] private TMP_InputField playerNameInput;    // プレイヤー名入力欄

    [Header("--- LANモード UI ---")]
    [SerializeField] private Toggle lanModeToggle;            // LANモード切替
    [SerializeField] private TextMeshProUGUI localIpText;     // 自分のIP表示用
    [SerializeField] private TMP_InputField targetIpInput;    // 相手のIP入力用
    [SerializeField] private Button lanHostButton;            // LANホストとして部屋を作るボタン
    [SerializeField] private Button lanJoinButton;            // LANクライアントとして部屋に入るボタン
    [SerializeField] private GameObject ugsInputGroup;        // UGS用の入力UI群（合言葉など）
    [SerializeField] private GameObject lanInputGroup;        // LAN用の入力UI群（相手IPなど）

    [Header("--- ルーム内 UI ---")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI[] playerListTexts;      // プレイヤー名の表示枠（2人分想定）
    [SerializeField] private Button startGameButton;      // 次のシーンへ（ホストのみ）
    [SerializeField] private Button leaveRoomButton;      // ロビー退出

    [Header("--- ローディングUI ---")]
    [SerializeField] private Button cancelLoadingButton;

    [Header("サインインUI")]
    [SerializeField] private GameObject signInPanel; // サインインパネル
    [SerializeField] private Button reConnectButton;  // 再接続ボタン
    [SerializeField] private Button offLineButton; // オフラインモードで起動するボタン

    // ==========================================
    // Presenterへ通知するためのイベント（Subject）
    // ==========================================

    // サインインリクエスト（UGSの匿名認証）
    private readonly Subject<Unit> _onSignInRequested = new();
    public Observable<Unit> OnSignInRequested => _onSignInRequested;

    // 誰かの部屋を探すリクエスト（UGSのカジュアルマッチ）
    private readonly Subject<Unit> _onJoinCasualMatchRequested = new();
    public Observable<Unit> OnJoinCasualMatchRequested => _onJoinCasualMatchRequested;

    // UGSの合言葉で部屋を作る or 入室するリクエスト
    private readonly Subject<Unit> _onJoinPrivateMatchRequested = new();
    public Observable<Unit> OnJoinPrivateMatchRequested => _onJoinPrivateMatchRequested;

    // LANモードでのホストリクエスト（自分のIPを公開して入室させる）
    private readonly Subject<Unit> _onLanHostRequested = new();
    public Observable<Unit> OnLanHostRequested => _onLanHostRequested;

    // LANモードでの入室リクエスト（相手のIPを入力して入る）
    private readonly Subject<Unit> _onLanJoinRequested = new();
    public Observable<Unit> OnLanJoinRequested => _onLanJoinRequested;

    // 次のシーンへ進むリクエスト（ホストのみ）
    private readonly Subject<Unit> _onNextSceneRequested = new();
    public Observable<Unit> OnNextSceneRequested => _onNextSceneRequested;

    // ロビー退出 or ロードキャンセルのリクエスト（どちらも同じ処理に繋ぐ）
    private readonly Subject<Unit> _onCancelRequested = new();
    public Observable<Unit> OnCancelRequested => _onCancelRequested;

    // LANモードの切り替えトグルの変化を通知するSubject
    private readonly Subject<bool> _onLanModeToggled = new();
    public Observable<bool> OnLanModeToggled => _onLanModeToggled;

    // ==========================================
    // Presenterが取得するためのプロパティ
    // ==========================================
    public string RoomCodeText => roomCodeInput != null ? roomCodeInput.text : "";
    public string TargetIPInputFieldText => targetIpInput != null ? targetIpInput.text : "";

    private void Awake()
    {
        // UIボタンのクリックイベントをR3のSubjectに変換して発火
        reConnectButton.onClick.AddListener(() => _onSignInRequested.OnNext(Unit.Default));
        offLineButton.onClick.AddListener(() => ChangeOffLineMode());

        joinCasualButton.onClick.AddListener(() => _onJoinCasualMatchRequested.OnNext(Unit.Default));
        joinPrivateButton.onClick.AddListener(() => _onJoinPrivateMatchRequested.OnNext(Unit.Default));
        startGameButton.onClick.AddListener(() => _onNextSceneRequested.OnNext(Unit.Default));

        lanHostButton.onClick.AddListener(() => _onLanHostRequested.OnNext(Unit.Default));
        lanJoinButton.onClick.AddListener(() => _onLanJoinRequested.OnNext(Unit.Default));

        // 退出ボタンとキャンセルボタンはどちらも「キャンセル・切断」の処理に繋ぐ
        leaveRoomButton.onClick.AddListener(() => _onCancelRequested.OnNext(Unit.Default));
        cancelLoadingButton.onClick.AddListener(() => _onCancelRequested.OnNext(Unit.Default));

        // LANモードトグルの切り替え
        if (lanModeToggle != null)
        {
            lanModeToggle.onValueChanged.AddListener(isOn =>
            {
                SwitchInputUI(isOn);
                _onLanModeToggled.OnNext(isOn);
            });
            SwitchInputUI(lanModeToggle.isOn); // 初期状態を反映
        }
    }

    private void Start()
    {
        // ローカルに保存されたプレイヤー名をロードして、入力欄に反映する
        PlayerDataManager.Instance.LoadLocalPlayerData();

        if (playerNameInput != null)
        {
            playerNameInput.text = PlayerDataManager.Instance.LocalPlayerName;
        }

        playerNameInput.onValueChanged.AddListener(newName =>
        {
            //// プレイヤー名が空文字の場合はデフォルト名を設定する
            //if (string.IsNullOrWhiteSpace(newName))
            //{
            //    playerNameInput.text = PlayerDataManager.DefaultPlayerNamePrefix;
            //}

            PlayerDataManager.Instance.SaveLocalPlayerName(newName);

        });
    }

    private void OnDestroy()
    {
        // メモリリーク防止のためSubjectを破棄
        _onSignInRequested.Dispose();
        _onJoinCasualMatchRequested.Dispose();
        _onJoinPrivateMatchRequested.Dispose();
        _onNextSceneRequested.Dispose();
        _onCancelRequested.Dispose();
        _onLanModeToggled.Dispose();
    }

    // ==========================================
    // Viewの見た目を変更するメソッド群（Presenterから呼ばれる）
    // ==========================================

    /// <summary>
    /// LANモードとUGSモードで入力フィールドの表示を切り替える（Viewの自己完結ロジック）
    /// </summary>
    private void SwitchInputUI(bool isLanMode)
    {
        if (ugsInputGroup != null) ugsInputGroup.SetActive(!isLanMode);
        if (lanInputGroup != null) lanInputGroup.SetActive(isLanMode);

    }

    public void UpdateLocalIPText(string ipAddress)
    {
        if (localIpText != null) localIpText.text = $"IP: {ipAddress}";
    }

    public void OpenSignInPanelInteractable()
    {
        signInPanel.SetActive(true);

        CurtainManager.Instance.HideLoadingMessage();
    }

    public void EnableJoinButton()
    {
        joinCasualButton.interactable = true;
        joinPrivateButton.interactable = true;
        signInPanel.SetActive(false); // サインイン完了後は隠す

        CurtainManager.Instance.OpenAsync(GetType().Name).Forget();

    }

    public void ChangeOffLineMode()
    {
        signInPanel.gameObject.SetActive(false);
        lanModeToggle.isOn = true;

        CurtainManager.Instance.UpdateLoadingMessage("オフラインモードで起動中...");
        CurtainManager.Instance.OpenAsync(GetType().Name,1).Forget();
    }


    public void DisableCancelButton()
    {
        cancelLoadingButton.interactable = false;
        leaveRoomButton.interactable = false;
    }

    public void SetUIStateOnMatchingStart()
    {
        joinCasualButton.interactable = false;
        joinPrivateButton.interactable = false;
        if (lanModeToggle != null) lanModeToggle.interactable = false; // マッチング中はトグルをロック
    }

    public void ResetMatchUI()
    {
        mainPanel.SetActive(true);
        roomPanel.SetActive(false);
        if (lanModeToggle != null) lanModeToggle.interactable = true;
        cancelLoadingButton.interactable = true;
        leaveRoomButton.interactable = true;

        foreach (var t in playerListTexts)
        {
            if (t != null) t.text = "---";
        }
    }

    // --- ルーム画面 ---
    public void ShowRoomPanel()
    {
        mainPanel.SetActive(false);
        roomPanel.SetActive(true);
    }

    public void HideRoomPanel()
    {
        roomPanel.SetActive(false);
    }

    public void UpdateRoomName(string roomName)
    {
        if (roomNameText != null) roomNameText.text = roomName;
    }

    public void UpdatePlayerList(string playerName, int index)
    {
        // indexは1始まり (1P, 2P...) で渡される想定
        int arrayIndex = index - 1;
        if (arrayIndex >= 0 && arrayIndex < playerListTexts.Length)
        {
            playerListTexts[arrayIndex].text = playerName;
        }
    }

    public void SetNextSceneButtonActive(bool isActive)
    {
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(isActive);
            startGameButton.interactable = isActive;
        }
    }
}