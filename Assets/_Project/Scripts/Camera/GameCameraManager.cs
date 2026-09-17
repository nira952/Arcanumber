using R3;
using Unity.Cinemachine;
using UnityEngine;

public class GameCameraManager : MonoBehaviour
{
    // シングルトン処理
    #region Singleton Pattern
    public static GameCameraManager Instance { get; private set; }

    private static readonly Subject<Unit> onInitialized = new();
    public static Observable<Unit> OnInitialized => onInitialized;

    private void SetUpSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            onInitialized.OnNext(Unit.Default);
        }
        else
        {
            Destroy(gameObject);
        }
        Debug.Log("[GameCameraManager] Awake called. Instance set.");
    }
    #endregion


    [SerializeField] private CinemachineCamera stageCamera;
    [SerializeField] private CinemachineCamera zoomCamera;
    [SerializeField] private CinemachineTargetGroup targetGroup;  // ターゲットグループ

    private Transform playerTransform;  // ズームターゲットプレイヤー
    private bool isZoomed = false;
    private bool isReversed = false;

    private void Awake()
    {
        SetUpSingleton();
    }


    /// <summary>
    /// targetGroup にターゲットを重み 1、半径 1 で追加する。
    /// </summary>
    /// <param name="target">追加するターゲットの Transform。</param>
    public void RegisterTarget(Transform target)
    {
        targetGroup.AddMember(target, 1f, 1f); // 重み1、半径1で追加
    }

    /// <summary>
    /// targetGroup からターゲットを削除する。
    /// </summary>
    /// <param name="target"></param>
    public void UnregisterTarget(Transform target)
    {
        targetGroup.RemoveMember(target);
    }

    /// <summary>
    /// ズームの状態を更新
    /// </summary>
    public void SetZoom(bool value, Transform target = null)
    {
        isZoomed = value;
        if (target != null)
        {
            playerTransform = target;
        }
        UpdateCameraPriorities();
    }

    /// <summary>
    /// 反転の状態を更新
    /// </summary>
    public void SetReversed(bool value)
    {
        isReversed = value;
        UpdateCameraPriorities();
    }

    /// <summary>
    /// 優先度の切り替え
    /// </summary>
    private void UpdateCameraPriorities()
    {
        //ズームの状態に応じてカメラの優先度を切り替え
        stageCamera.Priority = isZoomed ? 0 : 10;
        zoomCamera.Priority = isZoomed ? 10 : 0;
        zoomCamera.Follow = playerTransform;

        //反転の状態に応じてカメラの回転を調整
        stageCamera.transform.rotation = isReversed ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;
        zoomCamera.transform.rotation = isReversed ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;
    }
}
