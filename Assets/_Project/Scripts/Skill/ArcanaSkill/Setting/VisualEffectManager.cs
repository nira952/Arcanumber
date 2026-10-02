using DG.Tweening;
using NaughtyAttributes;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ArcanaスキルのUIを管理するクラス
/// </summary>
public class VisualEffectManager : NetworkBehaviour
{

    #region Singleton
    public static VisualEffectManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // オフラインモードかどうかを判定
        isLocalMode = PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsLocalMode;

        localPlayerIndex = PlayerDataManager.Instance.GetMyLobbyIndex();
    }
    #endregion


    [Label("視界を暗くするパネル")][SerializeField] private CanvasGroup darkPanel;

    private bool isLocalMode = false;

    private int localPlayerIndex = -1;

    /// <summary>
    /// フェードでダークパネルを表示する
    /// </summary>
    public void ShowDarkPanel(int index,float effectDuration)
    {
        if (isLocalMode)
        {
            StartCoroutine(FadeInOutDarkPanel(effectDuration));
        }
        else
        {
            RequestDarkPanelServerRpc(index, effectDuration);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDarkPanelServerRpc(int playerIndex,float effectDuration)
    {
        RequestDarkPanelClientRpc(playerIndex, effectDuration);
    }


    [ClientRpc(RequireOwnership = false)]
    private void RequestDarkPanelClientRpc(int playerIndex, float effectDuration)
    {
        // ローカルプレイヤーのインデックスと一致する場合は処理をスキップ
        if (playerIndex == localPlayerIndex) return;

        StartCoroutine(FadeInOutDarkPanel(effectDuration));
    }

    /// <summary>
    /// 視界をだんだん暗くするパネルの表示・非表示を切り替える
    /// </summary>
    private IEnumerator FadeInOutDarkPanel(float effectDuration)
    {
        float fadeTime = 1f; // フェードイン・フェードアウトの時間（秒）
        darkPanel.gameObject.SetActive(true);

        // Dotweenを使ってフェードイン・フェードアウトのアニメーションを実行する
        darkPanel.DOFade(1f, fadeTime / 2f).SetEase(Ease.InOutQuad);

        // effectDuration秒待つ
        yield return new WaitForSeconds(effectDuration);

        // フェードアウト
        darkPanel.DOFade(0f, fadeTime).SetEase(Ease.InOutQuad);

        yield return new WaitForSeconds(fadeTime);

        darkPanel.gameObject.SetActive(false);

    }

    /// <summary>
    /// フェードでダークパネルを表示する
    /// </summary>
    public void ShowZoomPlayer(int index, float effectDuration)
    {
        if (isLocalMode)
        {
            Transform playerTransform = PlayerUtility.GetPlayerByIndex(index)?.transform;

            StartCoroutine(ZoomPlayerCamera(playerTransform, effectDuration));
        }
        else
        {
            RequestZoomPlayerServerRpc(index, effectDuration);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void RequestZoomPlayerServerRpc(int playerIndex, float effectDuration)
    {
        RequestZoomPlayerClientRpc(playerIndex, effectDuration);
    }


    [ClientRpc(RequireOwnership = false)]
    private void RequestZoomPlayerClientRpc(int playerIndex, float effectDuration)
    {
        // ローカルプレイヤーのインデックスと一致する場合は処理をスキップ
        if (playerIndex == localPlayerIndex) return;

        Transform playerTransform = PlayerUtility.GetPlayerByIndex(playerIndex)?.transform;

        StartCoroutine(ZoomPlayerCamera(playerTransform, effectDuration));
    }

    private IEnumerator ZoomPlayerCamera(Transform playerTransform, float effectDuration)
    {
        GameCameraManager.Instance.SetZoom(true,playerTransform);

        yield return new WaitForSeconds(effectDuration);

        GameCameraManager.Instance.SetZoom(false, playerTransform);
    }



}
