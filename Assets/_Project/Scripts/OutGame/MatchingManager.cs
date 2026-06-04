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



public class MatchingManager : MonoBehaviour
{
    [SerializeField] private TitleUIManager titleUIManager;

    private const int MaxPlayers = 4;

    private Lobby currentLobby;

    private const string RelayKey = "RelayJoinCode"; // ロビーにRelayコードを保存するためのキー

    private const string MatchTypeKey = "MatchType";

    private const string MatchTypePrivate = "Private";

    private const string MatchTypeCasual = "Casual";


    // ─── R3: ロビーの人数変更を通知するプロパティ ───
    private readonly SerializableReactiveProperty<int> _playerCount = new(0);
    public ReadOnlyReactiveProperty<int> PlayerCount => _playerCount;

    private readonly SerializableReactiveProperty<string> _currentLobbyName = new("");
    public ReadOnlyReactiveProperty<string> CurrentLobbyName => _currentLobbyName;


    // R3: 購読（イベント監視）をまとめて解除するためのゴミ箱
    private readonly CompositeDisposable _disposables = new();

    private CancellationToken destroyCancellationToken;



    private void Awake()
    {
        // UniTask: 破棄時キャンセルに統一
        destroyCancellationToken = this.GetCancellationTokenOnDestroy();

        // R3: クリック要求を非同期処理に接続
        if (titleUIManager != null)
        {

            _disposables.Add(titleUIManager.OnPrivateMatchRequested.Subscribe(_ => HandlePrivateMatchAsync().Forget()));

            _disposables.Add(titleUIManager.OnCasualMatchRequested.Subscribe(_ => HandleCasualMatchAsync().Forget()));

        }
        else
        {

            Debug.LogError("TitleUIManagerがアサインされていません。");

        }

    }



    private void Start()
    {
        // UIのObservableを設定する
        titleUIManager.MatchingManagerObservable(this);

        // Unity Servicesの初期化と匿名サインインを開始
        InitializeServicesAsync().Forget();

    }

    /// <summary>
    /// Unity Services 初期化と匿名認証を行う
    /// </summary>
    private async UniTask InitializeServicesAsync()
    {
        // 初期化とサインインが完了するまではボタンを無効化しておく
        titleUIManager.OpenLoadingPanel("サービスに接続中...");
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {

                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                Debug.Log($"サインイン完了: {AuthenticationService.Instance.PlayerId}");



                titleUIManager.UpdateLoadingStatus("サインイン完了");



                // 少し待つ（UIの更新が見えるように）

                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: destroyCancellationToken);

            }



            // サインインが完了したら、ロードパネルを閉じ、タイトルパネルを開く

            titleUIManager.CloseAllPanel();

            titleUIManager.OpenTitlePanel();

        }

        catch (Exception e)
        {

            Debug.LogError($"初期化エラー: {e.Message}");

        }

    }

    // プライベートマッチの処理を非同期で実行する
    private async UniTask HandlePrivateMatchAsync()
    {
        if (titleUIManager == null)
        {
            Debug.LogError("TitleUIManagerがアサインされていません。");
            return;
        }

        string rawInput = titleUIManager.RoomCodeText.Trim();

        // 合言葉の入力チェック
        if (string.IsNullOrEmpty(rawInput))
        {
            Debug.LogWarning("合言葉を入力してください。");
            return;
        }

        string targetLobbyName = rawInput;

        string matchingMessage = "ロビー作成中...";

        // マッチング処理中はボタンを無効化して多重クリックを防止
        titleUIManager.OpenLoadingPanel("マッチング中...");

        // 1. 同じ合言葉＆空きがあるロビーを探す
        Lobby existingLobby = await FindAvailableLobbyByName(targetLobbyName, MatchTypePrivate);

        if (existingLobby != null)
        {

            // 2. 空きがあるロビーがあった ＝ クライアントとして参加
            Debug.Log($"合言葉【{targetLobbyName}】のロビーが見つかりました。参加します...");

            matchingMessage = "ロビー参加中...";

            await JoinLobbyAndRelay(existingLobby);

        }
        else
        {
            // 3. ロビーがない/満員 ＝ 自分がホストになって新規作成
            Debug.Log($"合言葉【{targetLobbyName}】のロビーがない、または満員のため、新しく作成します...");

            matchingMessage = "ロビー作成中...";

            await CreateLobbyAndRelay(targetLobbyName, MatchTypePrivate);

        }


        titleUIManager.UpdateLoadingStatus(matchingMessage);

        // 数秒待つ
        await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: destroyCancellationToken);


        titleUIManager.CloseAllPanel();

        titleUIManager.OpenLobbyPanel();

    }



    // カジュアルマッチの処理を非同期で実行する
    private async UniTask HandleCasualMatchAsync()
    {

        titleUIManager.OpenLoadingPanel("マッチング中...");

        string matchingMessage = "ロビー作成中...";

        // 1. カジュアルマッチの空きロビーを探す
        Lobby existingLobby = await FindAvailableCasualLobby();

        if (existingLobby != null)
        {
            // 2. 空きがあるロビーがあった ＝ クライアントとして参加
            Debug.Log("カジュアルマッチのロビーが見つかりました。参加します...");

            matchingMessage = "ロビー参加中...";

            await JoinLobbyAndRelay(existingLobby);
        }
        else
        {
            // 3. 空きがない ＝ 自分がホストになって新規作成

            string newLobbyName = $"Casual_{Guid.NewGuid():N}";

            Debug.Log("カジュアルマッチのロビーがないため、新しく作成します...");

            matchingMessage = "ロビー作成中...";

            await CreateLobbyAndRelay(newLobbyName, MatchTypeCasual);

        }

        titleUIManager.UpdateLoadingStatus(matchingMessage);

        // 数秒待つ

        await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: destroyCancellationToken);

        titleUIManager.CloseAllPanel();

        titleUIManager.OpenLobbyPanel();

    }



    // 名前とマッチ種別を条件に空きロビーを検索するメソッド
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



            if (response.Results.Count > 0)

            {

                return response.Results[0];

            }

            return null;

        }

        catch (LobbyServiceException e)
        {

            Debug.LogError($"ロビー検索エラー: {e.Message}");

            return null;

        }

    }



    // カジュアルマッチ用の空きロビー検索
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

            if (response.Results.Count > 0)
            {

                return response.Results[0];

            }

            return null;

        }

        catch (LobbyServiceException e)

        {

            Debug.LogError($"ロビー検索エラー: {e.Message}");

            return null;

        }

    }



    // ホスト処理：Relayの部屋を作り、そのコードを持ったLobbyを建てる
    private async UniTask CreateLobbyAndRelay(string lobbyName, string matchType)
    {
        try
        {
            // ① 先にRelayでホストの枠を確保
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);

            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);


            // ② NGOのネットワークトランスポートにRelay情報をセット
            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();

            utp.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData

            );

            // ③ ロビーを作成し、カスタムデータとしてRelayの接続コードとカテゴリを埋め込む
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    {

                        RelayKey, new DataObject(

                            DataObject.VisibilityOptions.Member,

                            relayJoinCode)

                    },

                    {

                        MatchTypeKey, new DataObject(

                            DataObject.VisibilityOptions.Public,

                            matchType,

                            DataObject.IndexOptions.S1)

                    }

                }

            };



            // ロビー作成（ロビー名 ＝ 合言葉）
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MaxPlayers, options);

            // ロビー名を更新（プライベートマッチは合言葉を表示、カジュアルマッチは空文字などにする）
            if (matchType == MatchTypePrivate)
            {
                _currentLobbyName.Value = lobbyName;
            }
            else
            {
                _currentLobbyName.Value = "カジュアルマッチ";
            }

            _playerCount.Value = currentLobby.Players.Count;
            // 定期的にロビーの生存信号（ハートビート）を送る処理を開始（切断防止）
            HeartbeatLobbyAsync(currentLobby.Id, 15, destroyCancellationToken).Forget();

            // NGOホスト起動
            NetworkManager.Singleton.StartHost();

            Debug.Log($"ホストとして部屋【{lobbyName}】を作成しました！他のプレイヤーの参加を待っています。");

        }

        catch (Exception e)
        {
            Debug.LogError($"ホスト作成失敗: {e.Message}");

        }

    }


    // クライアント処理：Lobbyに入り、中にあるRelayコードを使って接続する
    private async UniTask JoinLobbyAndRelay(Lobby lobby)
    {
        try
        {
            // ロビーに入室する
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);

            // ロビー名を更新
            if (currentLobby.Data.TryGetValue(MatchTypeKey, out var matchTypeData) && matchTypeData.Value == MatchTypePrivate)
            {
                // ロビー名（合言葉）をセット
                _currentLobbyName.Value = currentLobby.Name; 
            }
            else
            {
                _currentLobbyName.Value = "カジュアルマッチ";
            }
                // 現在のロビーのプレイヤー数を取得
            _playerCount.Value = currentLobby.Players.Count;



            // ロビーに保存されているRelayの接続コードを取り出す
            string relayJoinCode = currentLobby.Data[RelayKey].Value;

            // 取り出したコードを使ってRelayに参加
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            // NGOにRelayのデータをセット
            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();

            utp.SetClientRelayData(

                joinAllocation.RelayServer.IpV4,

                (ushort)joinAllocation.RelayServer.Port,

                joinAllocation.AllocationIdBytes,

                joinAllocation.Key,

                joinAllocation.ConnectionData,

                joinAllocation.HostConnectionData

            );

            // NGOクライアント起動
            NetworkManager.Singleton.StartClient();

            Debug.Log($"クライアントとして部屋【{lobby.Name}】に参加成功しました！");

        }

        catch (Exception e)
        {

            Debug.LogError($"ロビー・Relay参加失敗: {e.Message}");

        }

    }



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
                // ロビーがすでに存在しない場合は、ループを完全に抜ける
                Debug.Log("ロビーが既に削除されているため、ハートビートを終了します。");
                break;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ハートビート送信失敗: {e.Message}");
            }

            // 次の送信まで待機（キャンセルされたら即座に抜ける）
            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(waitTimeSeconds), cancellationToken: cancellationToken).SuppressCancellationThrow();
            if (isCanceled) break;
        }
    }


    private void OnDestroy()
    {
        // R3購読の解放
        _disposables.Dispose();

        // アプリ終了時やシーン遷移時にロビーを退出・削除する
        if (currentLobby != null)
        {
            // ★ NetworkManager 自体が存在するか、および NGO が起動中かを安全にチェック
            bool isHost = NetworkManager.Singleton != null &&
                          NetworkManager.Singleton.IsListening &&
                          NetworkManager.Singleton.IsHost;

            if (isHost)
            {
                try
                {
                    LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id).AsUniTask().Forget();
                }
                catch (Exception e)
                {
                    Debug.LogError($"ロビー削除失敗: {e.Message}");
                }
            }
            else
            {
                // NetworkManagerが既に無くても、自分自身がLobbyのAPIを叩いて抜ける処理は安全に実行可能
                LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId).AsUniTask().Forget();
            }
        }
    }


}