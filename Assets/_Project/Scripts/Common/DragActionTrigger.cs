using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Graphicを使うために追加

public enum DragDirection
{
    LeftToRight,
    RightToLeft,
    BottomToTop,
    TopToBottom
}

public class DragActionTrigger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("判定の有効/無効")]
    [SerializeField] private bool _isInteractable = true;

    // 外部から値を変更するためのプロパティ
    public bool IsInteractable
    {
        get => _isInteractable;
        set
        {
            _isInteractable = value;
            UpdateRaycastTarget();
        }
    }

    [Header("ドラッグ（スワイプ）設定")]
    public DragDirection targetDirection = DragDirection.BottomToTop;
    public float swipeThreshold = 100f;
    public UnityEvent onDragTriggered;

    private Vector2 dragStartPosition;
    private Graphic targetGraphic;

    private void Awake()
    {
        targetGraphic = GetComponent<Graphic>();
        UpdateRaycastTarget(); // 初期状態を反映
    }

    private void UpdateRaycastTarget()
    {
        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = _isInteractable;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsInteractable) return;
        dragStartPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        Vector2 dragDelta = eventData.position - dragStartPosition;
        if (IsDragValid(dragDelta))
        {
            onDragTriggered?.Invoke();
        }
    }

    private bool IsDragValid(Vector2 dragDelta)
    {
        bool isHorizontal = Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y);
        switch (targetDirection)
        {
            case DragDirection.LeftToRight: return isHorizontal && dragDelta.x >= swipeThreshold;
            case DragDirection.RightToLeft: return isHorizontal && dragDelta.x <= -swipeThreshold;
            case DragDirection.BottomToTop: return !isHorizontal && dragDelta.y >= swipeThreshold;
            case DragDirection.TopToBottom: return !isHorizontal && dragDelta.y <= -swipeThreshold;
            default: return false;
        }
    }
}