using Unity.Cinemachine;
using UnityEngine;

public class GameCameraManager : SingletonMonoBehaviour<GameCameraManager>
{
    [SerializeField] CinemachineCamera stageCamera;
    [SerializeField] CinemachineCamera zoomCamera;

    Transform playerTransform;  //ズームターゲットプレイヤー

    bool isZoomed = false;
    bool isReversed = false;

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
