using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private TitleUIManager titleUIManager;

    [SerializeField] private string traningSceneName = "ArcanaSelectScene";

    private void Start()
    {
        // カーソルを再表示する
        Cursor.visible = true;

        NetWorkAudioManager.Instance.PlayLocal(BgmName.Title);
        titleUIManager.OnOpenTrainingSceneRequested.Subscribe(_ =>
        {
            // ローカルモードを有効にする
            PlayerDataManager.Instance.SetLocalMode(true);

            // トレーニングシーンに遷移する処理をここに追加
            GameSceneManager.Instance.LoadLocalScene(traningSceneName).Forget();
        }).AddTo(this);


        titleUIManager.OnOpenTutorialSceneRequested.Subscribe(_ =>
        {
            // ローカルモードを有効にする
            PlayerDataManager.Instance.SetLocalMode(true);
            // チュートリアルシーンに遷移する処理をここに追加
            GameSceneManager.Instance.LoadLocalScene("Tutorial").Forget();
        }).AddTo(this);
    }




}
