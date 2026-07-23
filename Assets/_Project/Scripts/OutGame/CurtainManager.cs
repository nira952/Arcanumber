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

            // もしシーンがTitle以外ならcurtainを開ける
            if (GameSceneManager.Instance.GetCurrentScene() != Scene.Title)
            {
                _activeCurtain.OpenAsync(default).Forget();
            }


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
    public async UniTask OpenAsync(string caller,float duration = 0.2f)
    {
        // duration分待機してからカーテンを開ける
        await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: destroyCancellationToken);

        if (_activeCurtain == null) return;
        Debug.Log($"[CurtainManager] OpenAsync called: {caller}");
        await _activeCurtain.OpenAsync(destroyCancellationToken);
    }

    public async UniTask CloseAsync(string message,string caller,float duration = 0.1f)
    {
        if (_activeCurtain == null) return;

        await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: destroyCancellationToken);

        _activeCurtain.UpdateLoadingMessage(message);
        Debug.Log($"[CurtainManager] CloseAsync called: {caller}");

        await _activeCurtain.CloseAsync(destroyCancellationToken);
    }

    public void UpdateLoadingMessage(string message)
    {
        if (_activeCurtain == null) return;
        _activeCurtain.UpdateLoadingMessage(message);
    }

    public void HideLoadingMessage()
    {
        if (_activeCurtain == null) return;
        _activeCurtain.HideLoaingMessage();
    }
}