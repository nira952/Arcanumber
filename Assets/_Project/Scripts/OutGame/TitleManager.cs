using UnityEngine;
using R3;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private TitleUIManager titleUIManager;

    [SerializeField] private string traningSceneName = "ArcanaSelectScene";

    private void Start()
    {
        NetWorkAudioManager.Instance.PlayLocal(BgmName.Title);
        titleUIManager.OnOpenTrainingSceneRequested.Subscribe(_ =>
        {
            // ローカルモードを有効にする
            PlayerDataManager.Instance.SetLocalMode(true);

            // トレーニングシーンに遷移する処理をここに追加
            GameSceneManager.Instance.LoadLocalScene(traningSceneName);
        }).AddTo(this);

    }




}
