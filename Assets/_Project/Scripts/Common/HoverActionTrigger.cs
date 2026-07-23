using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Graphicを使うために追加

public class HoverActionTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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

    [Header("ホバーイベント")]
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;

    private Graphic targetGraphic;

    private void Awake()
    {
        // ImageやTextなどのUIコンポーネントを取得
        targetGraphic = GetComponent<Graphic>();
        UpdateRaycastTarget(); // 初期状態を反映
    }

    // RaycastTargetを同期するメソッド
    private void UpdateRaycastTarget()
    {
        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = _isInteractable;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable) return;
        onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsInteractable) return;
        onHoverExit?.Invoke();
    }
}