using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private TextManager textManager;

    private void Start()
    {
        textManager.StartText();
    }

    private void Update()
    {
        // Escapeキーが押されたらタイトルシーンに移動する
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndTutorial();
        }
    }

    private void EndTutorial()
    {
        // チュートリアル終了時の処理をここに追加
        GameSceneManager.Instance.LoadLocalScene("TitleScene");
    }
}

