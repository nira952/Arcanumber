using NaughtyAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ArcanaスキルのUIを管理するクラス
/// </summary>
public class VisualEffectManager : SingletonMonoBehaviour<VisualEffectManager>
{
    
    [Label("視界を暗くするパネル")][SerializeField] private GameObject darkPanel;
    /// <summary>
    /// 視界をだんだん暗くするパネルの表示・非表示を切り替える
    /// </summary>
    public IEnumerator ShowDarkPanel(bool isFadeIn)
    {
        //イメージを取得
        Image panelImage = darkPanel.GetComponent<Image>();

        float startAlpha = isFadeIn ? 0f : 1f;
        float endAlpha = isFadeIn ? 1f : 0f;
        float elapsed = 0f;

        if (isFadeIn) darkPanel.SetActive(true);

        //フェードの進行に合わせて透明度を更新
        while (elapsed < GameConfig.FADE_DURATION)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / GameConfig.FADE_DURATION);
            //透明度を更新
            Color color = panelImage.color;
            color.a = currentAlpha;
            panelImage.color = color;
            yield return null;
        }

        //最後に確実にセット
        Color finalColor = panelImage.color;
        finalColor.a = endAlpha;
        panelImage.color = finalColor;

        if (!isFadeIn) darkPanel.SetActive(false);
    }
}
