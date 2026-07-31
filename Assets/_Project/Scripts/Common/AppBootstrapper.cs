using R3;
using UnityEngine;



/// <summary>
/// アプリケーションの起動時に、DontDestroyOnLoadのGameObjectを作成し、
/// 必要なManagerをAddComponentして初期化するクラス
/// </summary>
public static class AppBootstrapper
{
    // 初期化完了を通知するSubject（外部からは購読のみできるようにObservableにする）
    private static readonly Subject<Unit> onInitializedSubject = new();
    public static Observable<Unit> OnInitialized => onInitializedSubject;

    // 既に初期化が終わっているかどうかのフラグ
    public static bool IsInitialized { get; private set; } = false;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeApp()
    {
        // オブジェクトを作成してDontDestroyOnLoadにする
        GameObject root = new GameObject("DontDestroyRoot");
        Object.DontDestroyOnLoad(root);

        // 1. 順番にAddComponent
        var dataManager         = root.AddComponent<PlayerDataManager>();
        var gameSceneManager    = root.AddComponent<GameSceneManager>();
        var netWorkAudioManager = root.AddComponent<NetWorkAudioManager>();
        var curtainManager      = root.AddComponent<CurtainManager>();
        var loadManager         = root.AddComponent<LoadManager>();
        var assetLoader         = root.AddComponent<AssetLoader>();

        // 2. 意図した順番で初期化を実行

        dataManager.DebugSettings();

        loadManager.Initialize();


        // 3. すべて終わったらR3で通知
        IsInitialized = true;
        onInitializedSubject.OnNext(Unit.Default);
        onInitializedSubject.OnCompleted(); // 以降、追加で発火しない場合はCompletedを呼ぶのが安全
    }
}