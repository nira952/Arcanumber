using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R3;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class ArcanaUIManager : MonoBehaviour
{
    [SerializeField] private DragActionTrigger dragActionTrigger;
    [SerializeField] private TextMeshProUGUI arcanaEXText;
    [SerializeField] private Transform cardDisplayParent;
    [SerializeField] private ArcanaCard cardPrefab;
    [SerializeField] private List<ArcanaCard> arcanaCards = new List<ArcanaCard>();

    [Header("--- アニメーション用設定 ---")]
    [SerializeField] private GlowImage[] animationCards = new GlowImage[22];
    [SerializeField] private RectTransform animationParent;
    [SerializeField] private RectTransform animationDefaultParent;
    [SerializeField] private RectTransform arcanaCardParent;
    [SerializeField] private float parentRotationSpeed = 5f;
    [SerializeField] private float cardMoveDistance = 200f;
    [SerializeField] private float cardMoveDuration = 0.5f;
    [SerializeField] private float cardMoveInterval = 0.5f;

    [Header("--- スキップ用の設定 ---")]
    [SerializeField] private Button skipButton;

    private const float cardRotationAngle = 90f;
    private Vector3 arcanaCardParantOriginPos = Vector3.zero;
    private Sprite cardSprite;
    private Color glowColor;

    private CancellationTokenSource animationCts;
    private bool isAnimationRunning = false;

    // ▼ 現在選択されているカードを保持するための変数
    private ArcanaCard currentSelectedCard = null;

    // --- R3 イベントの公開 ---

    private Subject<Unit> _onAnimationStartSubject = new Subject<Unit>();
    public Observable<Unit> OnAnimationStarted => _onAnimationStartSubject;

    private Subject<Unit> _onSelectCardSubmit = new Subject<Unit>();
    public Observable<Unit> OnSelectCardSubmit => _onSelectCardSubmit;

    private readonly Subject<Arcana> _onCardSelectedSubject = new Subject<Arcana>();
    public Observable<Arcana> OnCardSelected => _onCardSelectedSubject;

    private void Awake()
    {
        arcanaCardParantOriginPos = arcanaCardParent.anchoredPosition;
        arcanaCardParent.anchoredPosition = new Vector3(2000, arcanaCardParantOriginPos.y, 0);

        dragActionTrigger.onDragTriggered.AddListener(() =>
        {
            dragActionTrigger.IsInteractable = false;
            StartAnimation();
            _onAnimationStartSubject.OnNext(Unit.Default);
        });

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
    }

    private void OnDestroy()
    {
        _onCardSelectedSubject.Dispose();
        animationCts?.Cancel();
        animationCts?.Dispose();
    }

    public void Initialize(Sprite cardSprite, Color glowColor)
    {
        this.cardSprite = cardSprite;
        this.glowColor = glowColor;
    }

    private void SetArcanaEXText(string text)
    {
        if (arcanaEXText != null)
        {
            arcanaEXText.text = text;
        }
    }

    public void BuildCardsUI(IEnumerable<ArcanaSelectManager.ArcanaCard> myCards, List<Arcana> arcanaDatabase, Sprite backSprite)
    {
        foreach (Transform child in cardDisplayParent)
        {
            Destroy(child.gameObject);
        }

        arcanaCards.Clear();
        currentSelectedCard = null; // リセット時に選択状態も初期化
        SetArcanaEXText(""); // テキストも初期化

        foreach (var card in myCards)
        {
            Arcana arcanaData = arcanaDatabase.FirstOrDefault(a =>
                a.GetArcanaListID() == (int)card.CardId && a.GetIsFront() == card.IsFace);

            if (arcanaData == null) continue;

            ArcanaCard cardPrefabInstance = Instantiate(cardPrefab, cardDisplayParent);
            arcanaCards.Add(cardPrefabInstance);
            cardPrefabInstance.SetCardInfo(arcanaData.GetIsFront(), arcanaData.GetArcanaImage(), backSprite, glowColor);

            // スケール値の定義
            Vector3 defaultScale = Vector3.one;
            Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);

            // ① クリック時の処理
            cardPrefabInstance.selectButton.onClick.AddListener(() =>
            {
                if (currentSelectedCard == cardPrefabInstance)
                {
                    // 同じカードを再クリックした場合: 選択状態を解除
                    currentSelectedCard = null;
                    cardPrefabInstance.ResetCard();
                    cardPrefabInstance.transform.DOKill();
                    cardPrefabInstance.transform.DOScale(defaultScale, 0.2f);
                    SetArcanaEXText("");

                    _onCardSelectedSubject.OnNext(null);
                }
                else
                {
                    // 別のカードが選択されていた場合: 元に戻す
                    if (currentSelectedCard != null)
                    {
                        currentSelectedCard.ResetCard();
                        currentSelectedCard.transform.DOKill();
                        currentSelectedCard.transform.DOScale(defaultScale, 0.2f);
                    }

                    // 新しいカードを選択状態にする
                    currentSelectedCard = cardPrefabInstance;
                    cardPrefabInstance.transform.DOKill();
                    cardPrefabInstance.transform.DOScale(hoverScale, 0.2f);
                    SetArcanaEXText(arcanaData.GetArcanaEX());

                    _onCardSelectedSubject.OnNext(arcanaData);
                }
            });

            // ② ホバー（カーソルかざし）時の処理
            cardPrefabInstance.selectHoverTrigger.onHoverEnter.AddListener(() =>
            {
                // 何かが選択されていて、かつそれが「自分以外」ならホバー演出を無効化
                if (currentSelectedCard != null && currentSelectedCard != cardPrefabInstance) return;

                cardPrefabInstance.transform.DOKill();
                cardPrefabInstance.transform.DOScale(hoverScale, 0.2f);
                SetArcanaEXText(arcanaData.GetArcanaEX());
            });

            // ③ ホバー解除時の処理 (selectHoverTrigger に onHoverExit が定義されている想定)
            cardPrefabInstance.selectHoverTrigger.onHoverExit.AddListener(() =>
            {
                // 他のカードが選択中なら処理を無視
                if (currentSelectedCard != null && currentSelectedCard != cardPrefabInstance) return;

                // 自分が選択中の場合はスケールとテキストを維持
                if (currentSelectedCard == cardPrefabInstance) return;

                // 未選択状態なら元のスケールに戻し、テキストを消す
                cardPrefabInstance.transform.DOKill();
                cardPrefabInstance.transform.DOScale(defaultScale, 0.2f);
                SetArcanaEXText("");
            });

            // ④ 確定ボタンなどがあった場合
            cardPrefabInstance.OnCardSubmit += () =>
            {
                _onSelectCardSubmit.OnNext(Unit.Default);
            };
        }
    }

    public void StartAnimation()
    {
        // 既に動いていたら一旦キャンセル
        animationCts?.Cancel();
        animationCts?.Dispose();
        animationCts = new CancellationTokenSource();

        StartArcanaAnimation(animationCts.Token).Forget();
    }

    private async UniTask StartArcanaAnimation(CancellationToken token)
    {
        isAnimationRunning = true;

        // アニメーション開始のタイミングでスキップボタンを表示する
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
        }

        // 親オブジェクトは常時回転させる（後ほど一括KillできるようIDを設定しておきます）
        animationParent.DORotate(new Vector3(0, 0, 360), parentRotationSpeed, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear)
            .SetId("ArcanaAnimationTweens");

        for (int i = 0; i < animationCards.Length; i++)
        {
            int index = i;
            // カードの回転と移動を同時に行う
            animationCards[index].rectTransform.DORotate(
                new Vector3(0, 0, cardRotationAngle),
                cardMoveDuration
            ).SetId("ArcanaAnimationTweens");

            animationCards[index].rectTransform.DOAnchorPosX(
                animationCards[index].rectTransform.anchoredPosition.x + cardMoveDistance,
                cardMoveDuration
            ).SetId("ArcanaAnimationTweens")
            .OnComplete(() =>
            {
                // 親子付け
                animationCards[index].rectTransform.SetParent(animationParent, true);
            });

            // Delay時にキャンセルを検知できるように token を渡す
            await UniTask.Delay(System.TimeSpan.FromSeconds(cardMoveInterval), cancellationToken: token);
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(2f), cancellationToken: token);

        // ランダムに選んだカードを記録しておき、スキップ時にも同じカードを処理できるようにします
        List<int> chosenIndices = new List<int>();

        for (int i = 0; i < 5; i++)
        {
            int randomIndex = Random.Range(0, animationCards.Length);
            chosenIndices.Add(randomIndex);

            animationCards[randomIndex].sprite = cardSprite;
            animationCards[randomIndex].glowColor = glowColor;

            await UniTask.Delay(System.TimeSpan.FromSeconds(0.3f), cancellationToken: token);

            animationCards[randomIndex].rectTransform.SetParent(animationDefaultParent, true);
            animationCards[randomIndex].rectTransform.DOAnchorPosY(1000f, 0.5f).SetId("ArcanaAnimationTweens");

            await UniTask.Delay(System.TimeSpan.FromSeconds(0.2f), cancellationToken: token);
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(1f), cancellationToken: token);

        // 回転しているオブジェクトを画面外の上に移動させる
        animationDefaultParent.DOAnchorPosY(-300, 0.5f).SetId("ArcanaAnimationTweens")
        .OnComplete(() =>
        {
            animationDefaultParent.DOAnchorPosY(2000f, 1f).SetEase(Ease.InOutQuad)
            .SetId("ArcanaAnimationTweens")
            .OnComplete(() =>
            {
                animationParent.gameObject.SetActive(false);
            });
        });

        await UniTask.Delay(System.TimeSpan.FromSeconds(1f), cancellationToken: token);

        // 全てのアニメーションが正常終了したらスキップ終了処理へ（通常ルート）
        FinishAnimation(false);
    }

    /// <summary>
    /// スキップボタンが押された時の処理
    /// </summary>
    private void OnSkipButtonClicked()
    {
        if (!isAnimationRunning) return;

        // 1. 実行中の非同期処理(UniTask.Delay)を強制中断
        animationCts?.Cancel();
        animationCts?.Dispose();
        animationCts = null;

        // 2. この演出に関するDOTweenをすべて即座にKill（終了）する
        DOTween.Kill("ArcanaAnimationTweens");

        // 3. アニメーション完了後の状態を強制的に再現
        ForceCompleteState();

        // 4. 最後の「親オブジェクトを元の位置に戻す」アニメーションだけを再生
        FinishAnimation(true);
    }

    /// <summary>
    /// 途中アニメーションをすっ飛ばして、最終的な絵的な整合性を合わせる
    /// </summary>
    private void ForceCompleteState()
    {
        // animationParent を非表示にし、defaultParentは画面外の上(2000)に持っていく
        animationParent.gameObject.SetActive(false);
        animationDefaultParent.anchoredPosition = new Vector2(animationDefaultParent.anchoredPosition.x, 2000f);

        // 必要に応じて、動かしたカードの最終親子関係や位置の辻褄合わせ
        for (int i = 0; i < animationCards.Length; i++)
        {
            // アニメーション完了状態（全て親が animationParent に移っている、または上に射出されている）を再現
            if (animationCards[i].rectTransform.parent != animationDefaultParent)
            {
                animationCards[i].rectTransform.SetParent(animationParent, true);
            }
        }
    }

    /// <summary>
    /// 最後のカードを元に戻す演出を行い、後片付けをする
    /// </summary>
    private void FinishAnimation(bool wasSkipped)
    {
        isAnimationRunning = false;

        // スキップボタンを非表示にする
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        // アルカナカードの親オブジェクトを元の位置に戻すアニメーション（ここだけは必ず再生される）
        float duration = wasSkipped ? 0.8f : 1f; // スキップ時は少し早めにしても気持ちいいです
        arcanaCardParent.DOAnchorPosX(arcanaCardParantOriginPos.x, duration).SetEase(Ease.InOutQuad);
    }
}