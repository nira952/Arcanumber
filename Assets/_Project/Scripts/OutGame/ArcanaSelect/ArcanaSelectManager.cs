using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;

public class ArcanaSelectManager : NetworkBehaviour
{
    // アルカナの構造体（NGO通信用）
    public struct ArcanaCard : INetworkSerializable, IEquatable<ArcanaCard>
    {
        public int CardId;   // 0 ～ 21 (計22種類)
        public bool IsFace;  // true: 表, false: 裏

        public ArcanaCard(int id, bool isFace)
        {
            CardId = id;
            IsFace = isFace;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref CardId);
            serializer.SerializeValue(ref IsFace);
        }

        public bool Equals(ArcanaCard other)
        {
            return CardId == other.CardId && IsFace == other.IsFace;
        }
    }

    private const int CardsPerPlayer = 5;
    private const int TotalUniqueCards = 22;

    // 各プレイヤー（ClientId）に割り当てられたカードリスト（サーバー保持）
    private Dictionary<ulong, List<ArcanaCard>> _playerAllocations = new Dictionary<ulong, List<ArcanaCard>>();

    // クライアント自身が引いたカードの保持用
    private List<ArcanaCard> _myCards = new List<ArcanaCard>();

    // AssetLoaderからロードしたアルカナデータのリスト
    private List<Arcana> arcanaDatabase = new List<Arcana>();

    [Header("UI References")]
    [SerializeField] private GameObject arcanaUiPanel;       // カードを表示するUIパネル等の参照
    [SerializeField] private Transform cardDisplayParent;     // カードを表示する親オブジェクトのTransform参照
    [SerializeField] private SelectCard cardPrefab;             // カードのプレハブ参照（UI表示用）
    [SerializeField] private GameObject waitingOverlay;

    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        arcanaDatabase = AssetLoader.LoadAllArcanas();
        if (IsServer) SetupGamePositions();

        // 【R3による監視】ホスト（サーバー）のみ、全員が準備完了になったかを NetworkList から毎フレーム監視する
        if (IsServer)
        {
            // NetworkListの変化イベントをObservable化して購読（R3の機能）
            Observable.FromEvent<NetworkList<PlayerNetworkData>.OnListChangedDelegate, NetworkListEvent<PlayerNetworkData>>(
                h => (ev) => h(ev),
                h => PlayerDataManager.Instance.AllPlayerData.OnListChanged += h,
                h => PlayerDataManager.Instance.AllPlayerData.OnListChanged -= h
            )
            .Subscribe(_ =>
            {
                CheckAllPlayersReadyAndTransition().Forget(); 
            })
            .AddTo(_disposables);
        }
    }
    private void OnDestroy()
    {
        _disposables.Dispose(); // メモリリーク防止
    }

    private void OnCardSelected(Arcana selectedArcana)
    {
        // 1. ローカルに保存
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetLocalArcana(selectedArcana);
        }

        // 2. アルカナIDをサーバーへ通知
        SubmitSelectedArcanaServerRpc(selectedArcana.GetArcanaListID());

        // 3. 画面を待機中状態にする
        if (arcanaUiPanel != null) arcanaUiPanel.SetActive(false);
        if (waitingOverlay != null) waitingOverlay.SetActive(true);

        // 4. サーバーへ「自分は準備完了（待機中）になった」と通知
        SetReadyStatusServerRpc(true);
    }

    /// <summary>
    /// クライアントからサーバーへ、自身の準備状態を伝えるRPC
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SetReadyStatusServerRpc(bool isReady, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.Server_SetPlayerReady(clientId, isReady);
        }
    }

    /// <summary>
    /// 【サーバー専用】全員が準備完了しているか確認し、完了していればUniTaskで安全にシーン遷移する
    /// </summary>
    private async UniTaskVoid CheckAllPlayersReadyAndTransition()
    {
        var playerDataList = PlayerDataManager.Instance.AllPlayerData;

        // 部屋に誰もいない、またはデータがまだ構築されていない時は無視
        if (playerDataList.Count == 0) return;

        // 全員が IsReady == true かどうかをチェック
        foreach (var player in playerDataList)
        {
            if (!player.IsReady)
            {
                // 一人でも準備ができていなければ処理を中断
                Debug.Log("[Server] まだ全員が準備完了ではありません。");
                return;
            }
        }

        Debug.Log("[Server] 全プレイヤーが準備完了になりました。1秒後に SkillSelectScene に移行します。");

        // 連打やチラつき防止のため、UniTaskでほんの少しだけディレイを入れる（任意）
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

        // NGOのSceneManagerを使って、同期を保ったまま全クライアントを指定シーンへ強制遷移
        GameSceneManager.Instance.LoadNetworkScene("SkillSelect");
    }

    public void SetupGamePositions()
    {
        if (!IsServer) return;
        _playerAllocations.Clear();
        List<int> baseDeck = new List<int>();
        for (int i = 0; i < TotalUniqueCards; i++) baseDeck.Add(i);
        Shuffle(baseDeck);
        int deckIndex = 0;

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            List<ArcanaCard> assignedCards = new List<ArcanaCard>();
            for (int i = 0; i < CardsPerPlayer; i++)
            {
                if (deckIndex >= baseDeck.Count) break;
                int cardId = baseDeck[deckIndex++];
                bool isFace = UnityEngine.Random.value > 0.5f;
                assignedCards.Add(new ArcanaCard(cardId, isFace));
            }
            _playerAllocations[clientId] = assignedCards;
        }
    }

    public void OnCardOpenButtonPressed() => RequestCardDrawServerRpc();

    [ServerRpc(RequireOwnership = false)]
    private void RequestCardDrawServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (_playerAllocations.TryGetValue(clientId, out List<ArcanaCard> cards))
        {
            ArcanaCard[] cardsArray = cards.ToArray();
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            };
            TargetSendCardsClientRpc(cardsArray, clientRpcParams);
        }
    }

    [ClientRpc]
    private void TargetSendCardsClientRpc(ArcanaCard[] assignedCards, ClientRpcParams rpcParams = default)
    {
        _myCards = new List<ArcanaCard>(assignedCards);
        DisplayCardsUI();
    }

    private void DisplayCardsUI()
    {
        if (arcanaUiPanel != null) arcanaUiPanel.SetActive(true);
        foreach (Transform child in cardDisplayParent) Destroy(child.gameObject);

        foreach (var card in _myCards)
        {
            Arcana arcanaData = FindArcanaData((ArcanaList)card.CardId, card.IsFace);
            if (arcanaData == null) continue;

            SelectCard cardPrefabInstance = Instantiate(cardPrefab, cardDisplayParent);
            cardPrefabInstance.cardNameText.text = arcanaData.GetArcanaName;
            cardPrefabInstance.faceText.text = arcanaData.GetIsFront() ? "正位置" : "逆位置";
            cardPrefabInstance.setButton.onClick.AddListener(() => OnCardSelected(arcanaData));
        }
    }


    /// <summary>
    /// クライアントからサーバーへ、確定したアルカナの種類を送信するRPC
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SubmitSelectedArcanaServerRpc(int arcanaId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (PlayerDataManager.Instance != null)
        {
            // PlayerDataManager 側のサーバー同期処理を呼び出す
            PlayerDataManager.Instance.Server_UpdatePlayerArcana(clientId, (ArcanaList)arcanaId);
        }
    }

    private Arcana FindArcanaData(ArcanaList listType, bool isFront)
    {
        if (arcanaDatabase == null) return null;
        return arcanaDatabase.Find(arcana => arcana.GetArcanaListID() == (int)listType && arcana.GetIsFront() == isFront);
    }

    private void Shuffle(List<int> list)
    {
        int n = list.Count;
        while (n > 1) { n--; int k = UnityEngine.Random.Range(0, n + 1); int value = list[k]; list[k] = list[n]; list[n] = value; }
    }
}