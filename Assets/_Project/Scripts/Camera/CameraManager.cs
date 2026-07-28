using UnityEngine;

public class CameraManager : SingletonMonoBehaviour<CameraManager>
{
    [SerializeField] private Camera mainCamera; // メインカメラの参照


    /// <summary>
    /// メインカメラを取得する
    /// </summary>
    /// <returns></returns>
    public Camera GetMainCamera() { return mainCamera; }
}
