using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArcanaSelectManager : NetworkBehaviour
{
    // アルカナの構造体（NGOで同期・通信するためにINetworkSerializableを実装）
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

    // 1プレイヤーあたりに配る枚数
    private const int CardsPerPlayer = 5;
    private const int TotalUniqueCards = 22;

    // 各プレイヤー（ClientId）に割り当てられたカードリスト（サーバーのみが決定・保持し、RPCで送る）
    private Dictionary<ulong, List<ArcanaCard>> _playerAllocations = new Dictionary<ulong, List<ArcanaCard>>();

    // クライアント自身が引いたカードの保持用（ローカルでのUI表示用）
    private List<ArcanaCard> _myCards = new List<ArcanaCard>();

    [Header("Arcana Database")]
    // 事前に作成したすべてのArcana(ScriptableObject)をここに登録しておきます
    [SerializeField] private List<Arcana> arcanaDatabase = new List<Arcana>();

    [Header("Selected Target")]
    // 最終的に自分が選択・装備したアルカナデータ（確認用）
    [SerializeField] private Arcana myArcana;

    [Header("UI References")]
    [SerializeField] private GameObject arcanaUiPanel;       // カードを表示するUIパネル等の参照
    [SerializeField] private Transform cardDisplayParent;     // カードを表示する親オブジェクトのTransform参照
    [SerializeField] private SelectCard cardPrefab;             // カードのプレハブ参照（UI表示用）

    private void Start()
    {
        // ゲーム開始時にカード割り当てを行う（ホスト/サーバー側のみ）
        if (IsServer)
        {
            SetupGamePositions();
        }
    }

    /// <summary>
    /// ゲーム開始時（ホスト/サーバー側のみで実行）に、全プレイヤーのカードを事前に決定する
    /// </summary>
    public void SetupGamePositions()
    {
        if (!IsServer) return;

        _playerAllocations.Clear();

        // 1. 0〜21のカードIDの山札を作り、シャッフルする
        List<int> baseDeck = new List<int>();
        for (int i = 0; i < TotalUniqueCards; i++)
        {
            baseDeck.Add(i);
        }
        Shuffle(baseDeck);

        int deckIndex = 0;

        // 2. 接続されている全クライアントに対してカードを割り当てる
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            List<ArcanaCard> assignedCards = new List<ArcanaCard>();

            for (int i = 0; i < CardsPerPlayer; i++)
            {
                if (deckIndex >= baseDeck.Count)
                {
                    Debug.LogError("山札のカードが不足しています。");
                    break;
                }

                int cardId = baseDeck[deckIndex++];
                // 表裏をランダムで決定 (true = 表, false = 裏)
                bool isFace = UnityEngine.Random.value > 0.5f;

                assignedCards.Add(new ArcanaCard(cardId, isFace));
            }

            _playerAllocations[clientId] = assignedCards;
        }

        Debug.Log("[Server] 全プレイヤーのアルカナ割り当てが完了しました。");
    }

    /// <summary>
    /// クライアントが「カードを見る」ボタンを押したときに呼び出すUIイベント用関数
    /// </summary>
    public void OnCardOpenButtonPressed()
    {
        // サーバーに対して、自分のカード情報を要求する
        RequestCardDrawServerRpc();
    }

    /// <summary>
    /// クライアントからサーバーへ、カード配布を要求するRPC
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestCardDrawServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // もしカード割り当てが見つかったら
        if (_playerAllocations.TryGetValue(clientId, out List<ArcanaCard> cards))
        {
            // 配列に変換してクライアントへ送信
            ArcanaCard[] cardsArray = cards.ToArray();

            // 特定のクライアントにのみ送信するためのClientRpcParamsを作成
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };

            TargetSendCardsClientRpc(cardsArray, clientRpcParams);
        }
        else
        {
            Debug.LogWarning($"[Server] ClientId: {clientId} に対するカード割り当てが見つかりません。");
        }
    }

    /// <summary>
    /// サーバーから特定のクライアントへ、決定されたカードデータを送り返すRPC
    /// </summary>
    [ClientRpc]
    private void TargetSendCardsClientRpc(ArcanaCard[] assignedCards, ClientRpcParams rpcParams = default)
    {
        _myCards = new List<ArcanaCard>(assignedCards);

        Debug.Log($"[Client] カードを {_myCards.Count} 枚受信しました。画面に表示します。");

        // UIの表示処理をキック
        DisplayCardsUI();
    }

    /// <summary>
    /// 受信したカードデータを元に、実際に画面へカードを描画する処理
    /// </summary>
    private void DisplayCardsUI()
    {
        if (arcanaUiPanel != null)
        {
            arcanaUiPanel.SetActive(true);
        }

        // 古い表示カードが残っている場合はクリーンアップ
        foreach (Transform child in cardDisplayParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var card in _myCards)
        {
            // データベースから、一致するArcana(ScriptableObject)のデータを検索・取得
            Arcana arcanaData = FindArcanaData((ArcanaList)card.CardId, card.IsFace);

            if (arcanaData == null)
            {
                Debug.LogError($"ArcanaDatabaseに ID:{(ArcanaList)card.CardId}, 表裏:{card.IsFace} のデータが登録されていません。");
                continue;
            }

            // カードプレハブを生成してUIに追加
            SelectCard cardPrefabInstance = Instantiate(cardPrefab, cardDisplayParent);

            // Arcana(SO)が持つ、ローカライズされた日本語名や設定された画像・テキストをUIに適用
            cardPrefabInstance.cardNameText.text = arcanaData.GetArcanaName;
            cardPrefabInstance.faceText.text = arcanaData.GetIsFront() ? "正位置" : "逆位置";

            // DemoCard側にImageコンポーネント等があれば、以下のように画像も差し替え可能です
            if (arcanaData.GetArcanaImage() != null) cardPrefabInstance.cardImage.sprite = arcanaData.GetArcanaImage();

            // ボタンが押されたときのイベント登録 (該当のArcanaデータをそのまま渡す)
            cardPrefabInstance.setButton.onClick.AddListener(() => OnCardSelected(arcanaData));
        }
    }

    /// <summary>
    /// UI上のカードが選択された時の処理
    /// </summary>
    private void OnCardSelected(Arcana selectedArcana)
    {
        myArcana = selectedArcana;
        Debug.Log($"カードが選択されました: 名前={myArcana.GetArcanaName}, 位置={(myArcana.GetIsFront() ? "正位置" : "逆位置")}, カテゴリ={myArcana.GetASkillCategory()}");

        // 必要に応じて、ここで選択したカードのUIを閉じたり、確定処理をサーバーに送る(ServerRpc) などの処理を繋げます
        if (arcanaUiPanel != null)
        {
            arcanaUiPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 登録済みのArcanaリストから、ListIDと表裏が完全一致するアセットを探すヘルパー関数
    /// </summary>
    private Arcana FindArcanaData(ArcanaList listType, bool isFront)
    {
        if (arcanaDatabase == null) return null;

        return arcanaDatabase.Find(arcana =>
            arcana.GetArcanaListID() == (int)listType &&
            arcana.GetIsFront() == isFront
        );
    }

    /// <summary>
    /// フィッシャー〜イェーツのシャッフルアルゴリズム
    /// </summary>
    private void Shuffle(List<int> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            int value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}