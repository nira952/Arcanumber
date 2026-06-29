using UnityEngine;
using UnityEngine.EventSystems;

public class TrainingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject img;

    public void Start()
    {
        img.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        img.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        img.SetActive(false);
    }
}
