using DG.Tweening;
using TMPro;
using UnityEngine;

public class SkillSelectUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI skillExText;
    [SerializeField] private TextMeshProUGUI skillNameText;

    [SerializeField] private TextMeshProUGUI deleteButtonText;

    [SerializeField] private TextMeshProUGUI warningText;

    private void Awake()
    {
        skillExText.text = "";
        skillNameText.text = "";
        warningText.text = "";
        deleteButtonText.enabled = false;
    }

    public void SetSkillExText(string text,string name)
    {
        skillExText.text = text;
        skillNameText.text = name;
    }


    public void ShowDeleteText(bool show)
    {
        deleteButtonText.enabled = show;
    }


    public void ShowWarningText(string text)
    {
        warningText.text = text;

        // Dotweenでフェードインさせる
        warningText.DOFade(1f, 0.5f).OnComplete(() =>
        {
            // 2秒後にフェードアウト
            warningText.DOFade(0f, 0.5f).SetDelay(2f);
        });

    }

}
