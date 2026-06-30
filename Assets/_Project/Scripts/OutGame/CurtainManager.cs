using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CurtainManager : MonoBehaviour
{
    public static CurtainManager Instance { get; private set; }

    private CurtainController _curtainPrefab; 
    private CurtainController _activeCurtain;

    private void Awake()
    {
        // もし既にインスタンスが存在していれば、重複防止のために警告を出すか破棄する
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PlayerDataManager] 既にインスタンスが存在するため、重複したものを破棄します。");
            Destroy(this);
            return;
        }

        // 自分自身をインスタンスとして登録
        Instance = this;

        Initialize();
    }

    private void Initialize()
    {
        // ResourcesフォルダからカーテンUIのPrefabをロード
        _curtainPrefab = Resources.Load<CurtainController>("Prefabs/CurtainPrefab");

        if (_curtainPrefab == null)
        {
            Debug.LogError("CurtainUI PrefabがResourcesフォルダに存在しません。");
        }


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