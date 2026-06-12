using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyUIManager uiManager; // UI操作を委譲するViewへの参照

    private CancellationTokenSource _matchCancellationTokenSource;
    private bool _isPolling = false;
    private Lobby currentLobby;

    private const int MaxPlayers = 4;

    // UGS管理用のカスタムデータキー
    private const string RelayKey = "RelayJoinCode";
    private const string MatchTypeKey = "MatchType";
    private const string MatchTypePrivate = "Private";
    private const string MatchTypeCasual = "Casual";

    // R3: 購読（イベント監視）をまとめて解除するためのゴミ箱
    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        if (uiManager == null)
        {
            Debug.LogError("[DemoManager] UIManager がセットされていません。インスペクターを確認してください。");
            return;
        }

        // 初期UI状態のセット（ボタンの有効・無効、テキストの初期化）
        uiManager.ResetMatchUI();

        // Unity Services の初期化と匿名認証を非同期で開始
        InitializeServicesAsync().Forget();

        // R3のイベント購読を設定
        SettingObservable();
    }

    private void OnDestroy()
    {
        // 自身が破棄されるタイミングでR3のイベント購読をすべて自動解除（メモリリーク防止）
        _disposables.Dispose();
        _matchCancellationTokenSource?.Dispose();
    }

    /// <summary>
    /// R3を使ってUIManager（View）からのボタン入力を監視する
    /// </summary>
    private void SettingObservable()
    {
        // プライベートマッチの参加リクエストがあったとき
        uiManager.OnJoinPrivateMatchRequested
            .Subscribe(_ => HandleMatchAsync(MatchTypePrivate).Forget())
            .AddTo(_disposables);

        // カジュアルマッチの参加リクエストがあったとき
        uiManager.OnJoinCasualMatchRequested
            .Subscribe(_ => HandleMatchAsync(MatchTypeCasual).Forget())
            .AddTo(_disposables);

        // キャンセル・退室・解散ボタンが押されたとき
        uiManager.OnCancelRequested
            .Subscribe(_ => HandleCancelOrLeaveAsync().Forget())
            .AddTo(_disposables);

        // サインインリクエストがあったとき（通常は最初の1回だけ）
        uiManager.OnSignInRequested
            .Subscribe(_ => InitializeServicesAsync().Forget())
            .AddTo(_disposables);
    }

    /// <summary>
    /// Unity Services 初期化と匿名認証を行う
    /// </summary>
    private async UniTask InitializeServicesAsync()
    {
        try
        {
            InitializationOptions options = new InitializationOptions();

#if !UNITY_EDITOR
            // ビルドされたアプリ（エディタ以外）の時だけ、プロファイル名を変える
            options.SetProfile("Player_BuildClient");
#else
            // エディタ側も明示的に分けて同一PC内での複数起動での衝突を防ぐ
            options.SetProfile("Player_Editor");
#endif


            uiManager.ShowLoading("インターネットに接続中...");

            bool isConnected = await NetworkCheckUtility.CheckInternetConnectionAsync();

            if (!isConnected)
            {
                uiManager.UpdateLoadingMessage("インターネットに接続されていません。\n接続環境を確認してください。");

                uiManager.SetSignInButtonInteractable(true); // サインインボタンを再度押せるようにする

                return; // ここで処理を完全に終了（UGSのAPIを叩かせない）
            }

            uiManager.ShowLoading("サービスに接続中...");

            // 初期化
            await UnityServices.InitializeAsync(options);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[Services] サインイン完了: {AuthenticationService.Instance.PlayerId}");
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: destroyCancellationToken);
            }

            uiManager.HideLoading();

            // サインインが成功したらUIのメインボタンを押せるようにする
            uiManager.EnableJoinButton();
            uiManager.SetSignInButtonInteractable(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Services] 初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// マッチングエントリーのメインハンドラー（プライベート/カジュアルの自動判定分岐）
    /// </summary>
    private async UniTask HandleMatchAsync(string matchType)
    {
        uiManager.SetUIStateOnMatchingStart();
        uiManager.ShowLoading("インターネットに接続中...");

        bool isConnected = await NetworkCheckUtility.CheckInternetConnectionAsync();

        if (!isConnected)
        {
            Debug.LogError("[Lobby] インターネットに接続されていないため、マッチングを中断します。");

            uiManager.UpdateLoadingMessage("インターネットに接続されていません。\n接続環境を確認してください。");

            await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: destroyCancellationToken);

            uiManager.ResetMatchUI(); // UIを元の状態（ボタンが押せる状態）に戻す
            return; // ここで処理を完全に終了（UGSのAPIを叩かせない）
        }

        uiManager.ShowLoading("マッチング中...");


        // デフォルトネームはカジュアル
        string targetLobbyName = $"Casual_{Guid.NewGuid():N}";

        // マッチタイプがプライベートの場合
        if (matchType == MatchTypePrivate)
        {
            // UIManagerから現在のテキストボックスの入力を安全に取得
            string rawInput = uiManager.RoomCodeText.Trim();

            // 合言葉の入力チェック
            if (string.IsNullOrEmpty(rawInput))
            {
                Debug.LogWarning("合言葉を入力してください。");
                return;
            }

            // ロビーネームを更新
            targetLobbyName = rawInput;

        }


        // 今回のマッチングフロー専用のCancellationTokenSourceを再生成
        _matchCancellationTokenSource?.Dispose();
        _matchCancellationTokenSource = new CancellationTokenSource();

        // ゲームオブジェクト破棄（シーン遷移等）とユーザーキャンセルのどちらでも止まるようにリンク
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
            _matchCancellationTokenSource.Token,
            destroyCancellationToken
        ).Token;

        try
        {
            if (matchType == MatchTypePrivate)
            {
                // --- プライベートマッチの検索フロー ---
                Debug.Log($"[マッチング] 合言葉【{targetLobbyName}】のロビーを検索中...");
                uiManager.UpdateLoadingMessage($"合言葉【{targetLobbyName}】のロビーを検索中...");   
                Lobby existingLobby = await FindAvailableLobbyByName(targetLobbyName, matchType);
                linkedToken.ThrowIfCancellationRequested(); // 通通信間のキャンセルチェック

                if (existingLobby != null)
                {
                    Debug.Log("[マッチング] 空きロビー発見。ゲストとして参加します...");
                    uiManager.UpdateLoadingMessage("[マッチング] 空きロビー発見。ゲストとして参加します...");
                    await JoinLobbyAndRelay(existingLobby, linkedToken);
                }
                else
                {
                    Debug.Log("[マッチング] ロビーがないか満員のため、新規作成します...");
                    uiManager.UpdateLoadingMessage("[マッチング] ロビーがないか満員のため、新規作成します...");
                    await CreateLobbyAndRelay(targetLobbyName, matchType, linkedToken);
                }
            }
            else
            {
                // --- カジュアルマッチの検索フロー（同時衝突衝突リトライ付き） ---
                await HandleCasualMatchWithRetryAsync(linkedToken);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[Lobby] マッチング処理がユーザーによってキャンセルされました。");
            uiManager.UpdateLoadingMessage("[Lobby] マッチング処理がユーザーによってキャンセルされました。");

            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: destroyCancellationToken);

            uiManager.HideLoading();
            uiManager.ResetMatchUI();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby] マッチング重大エラー: {e.Message}");
            uiManager.ResetMatchUI();
        }

    }

    /// <summary>
    /// カジュアルマッチ用の衝突リトライ付き検索ループ
    /// </summary>
    private async UniTask HandleCasualMatchWithRetryAsync(CancellationToken cancellationToken)
    {
        int retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                Lobby existingLobby = await FindAvailableCasualLobby();

                if (existingLobby != null)
                {
                    Debug.Log($"[カジュアル] 空きロビー発見。参加を試みます... (トライ {retryCount + 1})");
                    await JoinLobbyAndRelay(existingLobby, cancellationToken);
                    return;
                }
                else
                {
                    Debug.Log($"[カジュアル] 空きロビーなし。新規作成を試みます... (トライ {retryCount + 1})");
                    string newLobbyName = $"Casual_{Guid.NewGuid():N}";

                    await CreateLobbyAndRelay(newLobbyName, MatchTypeCasual, cancellationToken);
                    return;
                }
            }
            catch (LobbyServiceException ex)
            {
                retryCount++;
                Debug.LogWarning($"[カジュアル] マッチング衝突またはエラーを検知。リトライします ({retryCount}/{maxRetries}): {ex.Message}");

                // 同時アクセスのタイミングをずらすためにランダムな時間（ジッター）待機
                await UniTask.Delay(UnityEngine.Random.Range(500, 1500), cancellationToken: cancellationToken);
            }
        }

        throw new Exception("規定回数リトライしましたが、カジュアルマッチの確立に失敗しました。");
    }

    /// <summary>
    /// プライベートロビー検索メソッド
    /// </summary>
    private async UniTask<Lobby> FindAvailableLobbyByName(string lobbyName, string matchType)
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 1,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.Name, lobbyName, QueryFilter.OpOptions.EQ),
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    new QueryFilter(QueryFilter.FieldOptions.S1, matchType, QueryFilter.OpOptions.EQ)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            return response.Results.Count > 0 ? response.Results[0] : null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby API] ロビー検索エラー: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// カジュアルロビー検索メソッド
    /// </summary>
    private async UniTask<Lobby> FindAvailableCasualLobby()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 1,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    new QueryFilter(QueryFilter.FieldOptions.S1, MatchTypeCasual, QueryFilter.OpOptions.EQ)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            return response.Results.Count > 0 ? response.Results[0] : null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby API] カジュアル検索エラー: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// ホスト処理：Relayの部屋を作り、その接続情報を持ったLobbyを建てる
    /// </summary>
    private async UniTask CreateLobbyAndRelay(string lobbyName, string matchType, CancellationToken cancellationToken)
    {
        // 安全のため、既存のNGOセッションがあれば切る
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[NGO] すでにNetworkManagerが稼働状態のため、一度シャットダウンします。");
            NetworkManager.Singleton.Shutdown();
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        // ① Relayホスト枠確保
        cancellationToken.ThrowIfCancellationRequested();
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);

        string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        // ② NGOへRelay情報を登録
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (utp == null) throw new Exception("NetworkManagerにUnityTransportコンポーネントが見つかりません！");

        utp.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );


        string playerName = PlayerDataManager.Instance.LocalPlayerName;

        // ホストプレイヤー名が空の場合か、デフォルト名の場合は、プレイヤー1とする
        if (string.IsNullOrEmpty(playerName) || playerName == PlayerDataManager.DefaultPlayerNamePrefix)
        {
            playerName = "Player1";
        }

        // ③ ロビーホストとしての自分のプロフィール設定
        var player = new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>
        {
            // 固定文字列ではなく、PlayerDataManagerからロード済みの名前を渡す
            { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,playerName) }
        });
        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Player = player,
            Data = new Dictionary<string, DataObject>
            {
                { RelayKey, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) },
                { MatchTypeKey, new DataObject(DataObject.VisibilityOptions.Public, matchType, DataObject.IndexOptions.S1) }
            }
        };

        // ④ ロビー作成API叩き
        cancellationToken.ThrowIfCancellationRequested();
        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MaxPlayers, options);

        // UI表示を反映
        if (matchType == MatchTypePrivate)
        {
            uiManager.UpdateRoomName($"合言葉: {lobbyName}");
        }
        else
        {
            uiManager.UpdateRoomName("カジュアルマッチ (待機中...)");
        }

        // ⑤ 定期維持通信（ハートビート）と、UI定期更新ループの起動
        HeartbeatLobbyAsync(currentLobby.Id, 15, cancellationToken).Forget();
        StartLobbyPollingLoopAsync(currentLobby.Id, cancellationToken).Forget();

        // ⑥ Netcode for GameObjects ホストとして起動
        if (!NetworkManager.Singleton.StartHost())
        {
            throw new Exception("NetworkManager.StartHostに失敗しました。");
        }

        // PlayerDataManager の手動ネットワーク同期処理
        if (PlayerDataManager.Instance != null)
        {
            var no = PlayerDataManager.Instance.GetComponent<NetworkObject>();
            if (no != null && !no.IsSpawned)
            {
                no.Spawn();
                Debug.Log("[NGO] PlayerDataManager を手動Spawnしました。");
            }
        }

        await UniTask.DelayFrame(3, PlayerLoopTiming.Update, cancellationToken);
        Debug.Log("[Lobby] ホスト作成、NGO起動完了。");
        uiManager.HideLoading();
        uiManager.ShowRoomPanel();
    }

    /// <summary>
    /// クライアント処理：Lobbyに入り、内部のRelayJoinCodeを使って接続する
    /// </summary>
    private async UniTask JoinLobbyAndRelay(Lobby lobby, CancellationToken cancellationToken)
    {

        string playerName = PlayerDataManager.Instance.LocalPlayerName;

        // ホストプレイヤー名が空の場合か、デフォルト名の場合は、プレイヤー1とする
        if (string.IsNullOrEmpty(playerName) || playerName == PlayerDataManager.DefaultPlayerNamePrefix)
        {
            // ゲストプレイヤー名が空の場合は、自分の現在のLobbyIndex+1とする（Player2など）
            playerName = $"Player{currentLobby.Players.Count + 1}";
        }

        // ① ゲストとしての自分のプロフィール設定
        JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions
        {
            Player = new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>
            {
                { 
                    "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) 
                }
            })
        };

        // ② ロビー入室API叩き
        cancellationToken.ThrowIfCancellationRequested();
        currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, joinOptions);
        Debug.Log("[Lobby] ロビー入室完了。");

        // UI表示更新
        if (currentLobby.Data.TryGetValue(MatchTypeKey, out var matchTypeData) && matchTypeData.Value == MatchTypePrivate)
        {
            uiManager.UpdateRoomName($"合言葉: {currentLobby.Name}");
        }
        else
        {
            uiManager.UpdateRoomName("カジュアルマッチ");
        }

        // ③ ロビー内のRelay接続コード抽出
        if (!currentLobby.Data.TryGetValue(RelayKey, out var relayData))
        {
            throw new Exception("ロビーデータ内にRelayコードが見つかりません。");
        }
        string relayJoinCode = relayData.Value;

        // ④ Relayサーバーへアロケーション参加依頼
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

        // ⑤ NGOへクライアント側のトランスポートデータセット
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData
        );

        // ⑥ クライアント接続完了を待つフック構築
        var tcs = new UniTaskCompletionSource<bool>();
        void OnClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId) tcs.TrySetResult(true);
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // クライアント起動
        if (!NetworkManager.Singleton.StartClient())
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            throw new Exception("NetworkManager.StartClientの起動自体に失敗。");
        }

        // 10秒の接続タイムアウト、または「ユーザーのキャンセル」の早い方で遮断
        var timeout = UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: cancellationToken);
        var winner = await UniTask.WhenAny(tcs.Task.AttachExternalCancellation(cancellationToken), timeout);

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

        if (winner.Equals(1)) // タイムアウト判定
        {
            NetworkManager.Singleton.Shutdown();
            throw new Exception("サーバーへの接続がタイムアウトしました。");
        }

        // ⑦ 接続成功したらUI更新ループを開始
        StartLobbyPollingLoopAsync(currentLobby.Id, cancellationToken).Forget();
        Debug.Log("[Lobby] クライアント接続フロー完了。");
        uiManager.HideLoading();
        uiManager.ShowRoomPanel();

    }

    /// <summary>
    /// ホストが部屋を維持し続けるためのハートビートパケット（15秒ごと送信）
    /// </summary>
    private async UniTask HeartbeatLobbyAsync(string lobbyId, float waitTimeSeconds, CancellationToken cancellationToken)
    {
        while (currentLobby != null && currentLobby.Id == lobbyId && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            }
            catch (LobbyServiceException ex) when (ex.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.Log("[Lobby] ロビーが既に削除されているため、ハートビートを終了します。");
                break;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Lobby] ハートビート送信失敗: {e.Message}");
            }

            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(waitTimeSeconds), cancellationToken: cancellationToken).SuppressCancellationThrow();
            if (isCanceled) break;
        }
    }

    /// <summary>
    /// ロビーの最新参加状況を定期取得してUI更新を回すループ（1.1秒ごと取得）
    /// </summary>
    private async UniTask StartLobbyPollingLoopAsync(string lobbyId, CancellationToken cancellationToken)
    {
        _isPolling = true;
        Debug.Log("[Lobby] UIの定期更新ポーリングループを開始しました。");

        while (_isPolling && currentLobby != null && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                currentLobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
                UpdateLobbyUI();
            }
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                // クライアントが切断を検知するメインポイント
                Debug.LogWarning("[Lobby] ホストによってロビーが解散されたことを検知しました。");

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                }
                currentLobby = null;
                _isPolling = false;
                uiManager.ResetMatchUI(); // UIを初期状態へ戻す
                break;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Lobby] ロビー情報更新ポーリング中にエラー: {e.Message}");
            }

            // UGSレートリミット制約を回避するため、安全に1.1秒の間隔を空ける
            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(1.1f), cancellationToken: cancellationToken).SuppressCancellationThrow();
            if (isCanceled) break;
        }
    }

    /// <summary>
    /// 【データ成形】現在のLobbyデータをもとに文字列を構築し、UIManager経由で反映する
    /// </summary>
    private void UpdateLobbyUI()
    {
        if (currentLobby == null) return;

        // 1. 部屋名（合言葉）と人数の表示データを組み立ててViewへ
        string roomText = $"合言葉: {currentLobby.Name} ({currentLobby.Players.Count}/{currentLobby.MaxPlayers})";
        uiManager.UpdateRoomName(roomText);

        int index = 1;

        foreach (var player in currentLobby.Players)
        {

            // 2. プレイヤーごとに、ホストかどうかの情報を付加した表示用の文字列を組み立ててViewへ
            if (player.Data != null && player.Data.TryGetValue("PlayerName", out var nameData))
            {
                string displayName = nameData.Value;
                if (player.Id == currentLobby.HostId)
                {
                    displayName += " (Host) ";
                }

                // UiManagerに名前を表示させる
                uiManager.UpdatePlayerList(displayName.ToString(), index);
            }

            index++;

        }
    }

    /// <summary>
    /// 【キャンセル・退室・解散ボタンの統合ロジック】
    /// </summary>
    private async UniTask HandleCancelOrLeaveAsync()
    {
        // 処理中の連打を防ぐために即座にキャンセルボタンを無効化
        uiManager.DisableCancelButton();

        // パターン1: まだLobbyの部屋自体に入っておらず、検索・接続中に押された場合
        if (currentLobby == null)
        {
            Debug.Log("[Lobby] マッチング中のため、タスクをキャンセル（中断）します。");
            uiManager.UpdateLoadingMessage("[Lobby] マッチング中のため、タスクをキャンセル（中断）します。");
            _matchCancellationTokenSource?.Cancel(); // 走っている非同期処理のトークンを全遮断
            return;
        }

        // ここから下は「すでにロビーの部屋に入っている状態」の処理
        _isPolling = false; // 定期取得ループを安全停止

        try
        {
            bool isHost = currentLobby.HostId == AuthenticationService.Instance.PlayerId;

            if (isHost)
            {
                // パターン2: 自分がホストの場合 ＝ ロビーを「解散（削除）」
                Debug.Log("[Lobby] ホストとしてロビーを解散（削除）します...");
                uiManager.UpdateLoadingMessage("[Lobby] ホストとしてロビーを解散（削除）します...");
                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
            }
            else
            {
                // パターン3: 自分がゲストの場合 ＝ ロビーから「自分を退室」
                Debug.Log("[Lobby] ゲストとしてロビーから自主退室します...");
                uiManager.UpdateLoadingMessage("[Lobby] ゲストとしてロビーから自主退室します...");
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Lobby API] 退出処理中にエラー（すでに部屋がない場合など）: {e.Message}");
        }
        finally
        {
            // NGO（Netcode通信）の完全シャットダウン
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[NGO] NetworkManagerをシャットダウンします。");
                uiManager.UpdateLoadingMessage("[NGO] NetworkManagerをシャットダウンします...");
                NetworkManager.Singleton.Shutdown();
            }

            currentLobby = null;

            // UIを初期状態にリセット
            uiManager.ResetMatchUI();
            uiManager.HideRoomPanel();
            uiManager.HideLoading();
            Debug.Log("[Lobby] 退出・解散処理がすべて正常完了しました。");
        }
    }

    // ホストの画面で「ゲーム開始」が押されたときの処理
    private async UniTask HandleStartGameAsync()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // 1. 現在の UGS Lobby に入っている最新のプレイヤーリストを取得
        var ugsPlayers = currentLobby.Players;

        // 2. PlayerDataManager に渡すための確定リストを作成
        var finalizedList = new System.Collections.Generic.List<PlayerNetworkData>();

        // NGOに現在接続しているクライアントIDのリストを取得
        var connectedClientIds = NetworkManager.Singleton.ConnectedClientsIds;

        for (int i = 0; i < ugsPlayers.Count; i++)
        {
            var ugsPlayer = ugsPlayers[i];

            // UGSのPlayerNameを取得 (なければデフォルト名)
            string pName = ugsPlayer.Data.TryGetValue("PlayerName", out var nameData) ? nameData.Value : $"Player{i + 1}";

            // 💡重要: UGSのプレイヤー順(i番目)と、NGOのClientIdを正しく対応させる
            // 通常は、接続順にConnectedClientsIdsに入っているのでそこから紐付けるか、
            // ロビー入室時に各々が自分のClientIdをLobbyデータに書き込んでおく等の方法があります。
            ulong matchedClientId = (i < connectedClientIds.Count) ? connectedClientIds[i] : (ulong)i;

            // ここで各プレイヤーにあらかじめ決めておいた色などを割り振る
            //Color assignedColor = GetSelectedColorForPlayer(i);

            var data = new PlayerNetworkData
            {
                LobbyIndex = i, // 0, 1, 2, 3 のインデックスを確定
                ClientId = matchedClientId,
                PlayerName = pName,
                //SelectedColor = assignedColor
            };

            finalizedList.Add(data);
        }

        // 3. PlayerDataManager にデータを流し込み、全クライアントへ同期（一瞬で同期されます）
        PlayerDataManager.Instance.Server_BuildAndSyncPlayerData(finalizedList);

        // 4. 同期が完了したのを見計らって、安全に次のシーンへ遷移
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f)); // 同期猶予をほんの少しだけ取る

        // NGOのSceneManagerを使って全員一斉にシーン遷移
        NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

}