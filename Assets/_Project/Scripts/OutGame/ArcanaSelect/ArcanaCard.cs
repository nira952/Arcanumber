using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ArcanaCard : MonoBehaviour
{
    public Action OnCardSubmit; // カード確定（ドラッグ等）時のイベント
    public Button selectButton;

    public HoverActionTrigger selectHoverTrigger;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject flontFaces;
    [SerializeField] private Image frontImage;
    [SerializeField] private Image backImage;
    [SerializeField] private float flipDuration = 0.4f;
    [SerializeField] private HoverActionTrigger flipHoverTrigger;
    [SerializeField] private DragActionTrigger dragTrigger;

    private bool isSelected = false;

    // ▼ アニメーションの衝突を防ぐためのキャッシュ

    private Tween moveTween = null;

    // ▼ カードの初期位置（ローカル座標 / アンカー座標）を記憶
    private Vector2 defaultAnchoredPos;
    private RectTransform frontRectTransform;

    void Start()
    {
        frontRectTransform = frontImage.rectTransform;
        // 初期位置を記憶（ここを基準にして上下に動かします）
        defaultAnchoredPos = frontRectTransform.anchoredPosition;

        flontFaces.SetActive(false);
        backImage.gameObject.SetActive(true);
        transform.localEulerAngles = Vector3.zero;
        selectHoverTrigger.IsInteractable = false;
        //frontImage.glowSize = 0f;
        isSelected = false;

        flipHoverTrigger.onHoverEnter.AddListener(FlipToFront);
        dragTrigger.onDragTriggered.AddListener(SelectCard);
        selectButton.onClick.AddListener(OnButtonClick);
        selectHoverTrigger.onHoverEnter.AddListener(OnEnterSelectHover);
        selectHoverTrigger.onHoverExit.AddListener(OnExitSelectHover);

        // 初期状態：裏面なのでホバーをON、ドラッグ（選択）をOFFにしておく
        if (flipHoverTrigger != null) flipHoverTrigger.IsInteractable = true;
        if (dragTrigger != null) dragTrigger.IsInteractable = false;
    }

    public void SetCardInfo(bool isFront, Sprite frontSprite, Sprite backSprite, Color glowColor)
    {
        frontImage.sprite = frontSprite;
        backImage.sprite = backSprite;
        //frontImage.glowColor = glowColor;
        //backImage.glowColor = glowColor;

        if (!isFront)
        {
            // カードが逆位置なら、flontImageをz180度回転させて逆さまにする。
            flontFaces.transform.localEulerAngles = new Vector3(0, 0, 180);
        }
    }

    // HoverActionTrigger の OnHoverEnter から呼ばれる（裏返し演出）
    public void FlipToFront()
    {
        DG.Tweening.Sequence currentSequence = DOTween.Sequence();

        currentSequence.Append(transform.DOScale(1.2f, flipDuration * 0.5f).SetEase(Ease.OutQuad));
        currentSequence.Append(transform.DOScale(1.0f, flipDuration * 0.5f).SetEase(Ease.InQuad));
        currentSequence.Join(transform.DORotate(new Vector3(0, 90, -5), flipDuration * 0.5f).SetEase(Ease.InQuad));

        currentSequence.AppendCallback(() =>
        {
            backImage.gameObject.SetActive(false);
            flontFaces.SetActive(true);
            transform.localEulerAngles = new Vector3(0, -90, -5);
        });

        currentSequence.Append(transform.DORotate(new Vector3(0, 0, 0), flipDuration * 0.5f).SetEase(Ease.OutQuad));

        currentSequence.OnComplete(() =>
        {
            // 表を向いたら：ホバーを有効にする
            if (flipHoverTrigger != null) flipHoverTrigger.IsInteractable = false;
            if (selectHoverTrigger != null) selectHoverTrigger.IsInteractable = true;
        });
    }

    // カードのボタンがクリックされた時の処理
    private void OnButtonClick()
    {
        if (isSelected)
        {
            // 【解除処理】すでに選択済みの場合は解除して元に戻す
            ResetCard();
        }
        else
        {
            // 【選択処理】選択状態にする
            isSelected = true;

            if (dragTrigger != null) dragTrigger.IsInteractable = true;

            //frontImage.glowSize = 10f;

            // スケールと位置のTweenをリセッ
            moveTween?.Kill();

            // 拡大表示 ＆ 基準位置から +20 浮き上がらせる
            moveTween = frontRectTransform.DOAnchorPosY(defaultAnchoredPos.y + 20f, 0.2f).SetEase(Ease.OutBack);
        }
    }

    private void OnEnterSelectHover()
    {
        // 選択中の場合は何もしない（選択中の見栄えを維持）
        if (isSelected) return;

        //frontImage.glowSize = 10f;

        moveTween?.Kill();

        // ホバー時の拡大表示 ＆ 少し浮かせる演出
        moveTween = frontRectTransform.DOAnchorPosY(defaultAnchoredPos.y + 10f, 0.15f).SetEase(Ease.OutQuad);
    }

    private void OnExitSelectHover()
    {
        // 選択中の場合はホバーが外れても元の位置・スケールに戻さない
        if (isSelected) return;

        //frontImage.glowSize = 0f;

        moveTween?.Kill();

        // 未選択状態へリセット
        moveTween = frontRectTransform.DOAnchorPosY(defaultAnchoredPos.y, 0.15f).SetEase(Ease.InQuad);
    }

    // 選択状態の解除、あるいは他のカードが選択された時にマネージャー側から呼ばれるリセット
    public void ResetCard()
    {
        isSelected = false;
        //frontImage.glowSize = 0f;

        if (dragTrigger != null) dragTrigger.IsInteractable = false;

        moveTween?.Kill();

        // 初期スケール ＆ 初期Y座標に確実に戻す
        moveTween = frontRectTransform.DOAnchorPosY(defaultAnchoredPos.y, 0.2f).SetEase(Ease.OutQuad);
    }

    // DragActionTrigger の OnDragTriggered から呼ばれる（カード確定時）
    private void SelectCard()
    {
        Debug.Log($"{gameObject.name} が確定（ドラッグ）されました！");

        transform.DOMoveY(transform.position.y + 300f, 0.3f).SetEase(Ease.InBack);
        canvasGroup?.DOFade(0f, 0.3f);

        OnCardSubmit?.Invoke();
    }
}