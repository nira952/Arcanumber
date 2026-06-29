using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CurtainManager : MonoBehaviour
{
    public static CurtainManager Instance { get; private set; }

    [SerializeField] private CurtainController _curtainPrefab; 
    private CurtainController _activeCurtain;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        if (_activeCurtain == null && _curtainPrefab != null)
        {
            // 管理オブジェクトの子としてカーテンUIを生成
            _activeCurtain = Instantiate(_curtainPrefab, transform);
        }
    }

    /// <summary>
    /// 外から呼び出す用：カーテンを閉める
    /// </summary>
    public async UniTask CloseAsync()
    {
        if (_activeCurtain == null) return;
        await _activeCurtain.CloseAsync(destroyCancellationToken);
    }

    /// <summary>
    /// 外から呼び出す用：カーテンを開ける
    /// </summary>
    public async UniTask OpenAsync()
    {
        if (_activeCurtain == null) return;
        await _activeCurtain.OpenAsync(destroyCancellationToken);
    }
}