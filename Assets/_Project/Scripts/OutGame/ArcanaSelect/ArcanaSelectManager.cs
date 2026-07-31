using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArcanaSelectManager : NetworkBehaviour
{
    private bool isLocalMode = false;

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

    private Dictionary<ulong, List<ArcanaCard>> _playerAllocations = new Dictionary<ulong, List<ArcanaCard>>();
    private List<ArcanaCard> _myCards = new List<ArcanaCard>();
    private List<Arcana> arcanaDatabase = new List<Arcana>();
    [SerializeField] private ArcanaUIManager arcanaUIManager;
    [SerializeField] private Sprite[] cardSprites = new Sprite[4];
    [SerializeField] private Color[] glowColors = new Color[4]; // 0:赤, 1:青, 2:緑, 3:黄

    private Arcana selectedArcana;
    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        // ローカルモードかどうかを判定
        isLocalMode = PlayerDataManager.Instance.IsLocalMode;
        arcanaDatabase = AssetLoader.Instance.LoadAllArcanas;


        // -------------------------
        // 通信初期化・サブスクライブ
        // -------------------------
        if (!isLocalMode)
        {
            if (IsServer) SetupGamePositions();

            if (IsServer)
            {
                Observable.FromEvent<NetworkList<PlayerNetworkData>.OnListChangedDelegate, NetworkListEvent<PlayerNetworkData>>(

                    h => (ev) => h(ev),

                    h => PlayerDataManager.Instance.AllPlayerData.OnListChanged += h,

                    h => PlayerDataManager.Instance.AllPlayerData.OnListChanged -= h

                )

                .Subscribe(_ => CheckAllPlayersReadyAndTransition().Forget())

                .AddTo(_disposables);

            }

        }

        else

        {

            Debug.Log("[ArcanaSelectManager] DebugModeが有効です。オフラインで動作します。");

        }



        // UIManagerの初期化 (デバッグ時はデフォルトで0番とするなどの配慮)

        int myLobbyIndex = isLocalMode ? 0 : PlayerDataManager.Instance.GetMyLobbyIndex();

        Sprite myBackSprite = cardSprites[Mathf.Clamp(myLobbyIndex, 0, cardSprites.Length - 1)];

        Color myGlowColor = glowColors[Mathf.Clamp(myLobbyIndex, 0, glowColors.Length - 1)];

        arcanaUIManager.Initialize(myBackSprite, myGlowColor);



        // --- R3によるUIイベントの結合 ---

        // 1. カード選択時の処理
        arcanaUIManager.OnCardSelected

            .Subscribe(HandleCardSelected)

            .AddTo(_disposables);


        // 2. 確定ボタン押下時の処理
        arcanaUIManager.OnSelectCardSubmit

            .Subscribe(_ => SubmitCardSetting())

            .AddTo(_disposables);

        // 3. カードオープンアニメーション開始時の処理
        arcanaUIManager.OnAnimationStarted

            .Subscribe(_ => CardOpen())

            .AddTo(_disposables);
    }



    // Viewから流れてきたArcanaデータを受け取る

    private void HandleCardSelected(Arcana arcana)
    {
        if (!isLocalMode && PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetLocalArcana(arcana);
        }

        arcanaUIManager.ShowSelectText(true);
        selectedArcana = arcana;
    }



    // 確定ボタンのロジック

    private void SubmitCardSetting()
    {
        if (selectedArcana == null) return; // 選択されていない場合のフェールセーフ

        CurtainManager.Instance.CloseAsync("待機中", GetType().Name, duration: 0.5f).Forget();

        if (isLocalMode)
        {
            // デバッグ時は直接ローカルで遷移処理を呼ぶ
            DebugTransitionAsync().Forget();
        }
        else
        {
            SubmitSelectedArcanaServerRpc(selectedArcana.GetArcanaListID());
            SetReadyStatusServerRpc(true);
        }

    }



    // --- デバッグ用：オフライン時のシーン遷移モック ---

    private async UniTaskVoid DebugTransitionAsync()

    {

        Debug.Log("[DebugMode] 確定されました。1秒後にローカルで SkillSelectScene に移行します。");

        await CurtainManager.Instance.CloseAsync("Ready!", GetType().Name, duration: 0.5f);

        await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

        GameSceneManager.Instance.LoadLocalScene("SkillSelect"); // オフライン用のシーン遷移

    }



    // クライアントからサーバーへ自身の準備状態を伝えるRPC

    [ServerRpc(RequireOwnership = false)]

    private void SetReadyStatusServerRpc(bool isReady, ServerRpcParams rpcParams = default)

    {

        ulong clientId = rpcParams.Receive.SenderClientId;

        if (PlayerDataManager.Instance != null)

        {

            PlayerDataManager.Instance.Server_SetPlayerReady(clientId, isReady);

        }

    }



    // 【サーバー専用】全員が準備完了しているか確認し、完了していれば遷移

    private async UniTaskVoid CheckAllPlayersReadyAndTransition()

    {

        var playerDataList = PlayerDataManager.Instance.AllPlayerData;

        if (playerDataList.Count == 0) return;



        foreach (var player in playerDataList)

        {

            if (!player.IsReady)

            {

                Debug.Log("[Server] まだ全員が準備完了ではありません。");

                return;

            }

        }



        Debug.Log("[Server] 全プレイヤーが準備完了になりました。1秒後に SkillSelectScene に移行します。");

        await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

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



    public void CardOpen()

    {

        if (isLocalMode)

        {

            // デバッグ時はサーバーへ問い合わせず、ローカルでカードを生成して表示する

            GenerateDebugCards();

        }

        else

        {

            RequestCardDrawServerRpc();

        }

    }



    // --- デバッグ用：オフライン時のカード生成モック ---

    private void GenerateDebugCards()

    {

        _myCards.Clear();

        for (int i = 0; i < CardsPerPlayer; i++)

        {

            int cardId = UnityEngine.Random.Range(0, TotalUniqueCards);

            bool isFace = UnityEngine.Random.value > 0.5f;

            _myCards.Add(new ArcanaCard(cardId, isFace));

        }



        Sprite backSprite = cardSprites[0]; // デバッグ用として0番を使用

        arcanaUIManager.BuildCardsUI(_myCards, arcanaDatabase, backSprite);

    }



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

        Sprite backSprite = cardSprites[PlayerDataManager.Instance.GetMyLobbyIndex()];

        arcanaUIManager.BuildCardsUI(_myCards, arcanaDatabase, backSprite);

    }



    // クライアントからサーバーへ、確定したアルカナの種類を送信するRPC

    [ServerRpc(RequireOwnership = false)]

    private void SubmitSelectedArcanaServerRpc(int arcanaId, ServerRpcParams rpcParams = default)

    {

        ulong clientId = rpcParams.Receive.SenderClientId;

        if (PlayerDataManager.Instance != null)

        {

            PlayerDataManager.Instance.Server_UpdatePlayerArcana(clientId, (ArcanaList)arcanaId);

        }

    }



    private void Shuffle(List<int> list)

    {

        int n = list.Count;

        while (n > 1) { n--; int k = UnityEngine.Random.Range(0, n + 1); int value = list[k]; list[k] = list[n]; list[n] = value; }

    }



    private void OnDestroy()

    {

        _disposables.Dispose();

    }

}

