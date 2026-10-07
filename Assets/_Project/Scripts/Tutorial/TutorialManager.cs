using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private TextManager textManager;

    [SerializeField] private PlayerSpawner playerSpawner;

    [SerializeField] private GameUIManager gameUIManager;

    [SerializeField] private GameObject tutorialGround;
    private void Start()
    {
        textManager.OnMoveStart
            .Subscribe(_ =>
            {
                PlayerMoveStart();
            });


        // チュートリアル用のスキルとアルカナを設定
        Skill[] tutorialSkills = new Skill[4];

        tutorialSkills[0] = AssetLoader.Instance.GetSkill(1); // スキル1を取得
        tutorialSkills[1] = AssetLoader.Instance.GetSkill(2); // スキル2を取得
        tutorialSkills[2] = AssetLoader.Instance.GetSkill(3); // スキル3を取得
        tutorialSkills[3] = AssetLoader.Instance.GetSkill(4); // スキル4を取得

        Arcana tutorialArcana = new Arcana();
        tutorialArcana = AssetLoader.Instance.GetArcana(ArcanaList.Fool,true); // アルカナを取得

        PlayerDataManager.Instance.SetLocalSkills(tutorialSkills);
        PlayerDataManager.Instance.SetLocalArcana(tutorialArcana);

        // プレイヤーのスポーン
        playerSpawner.SpawnAllPlayersOffline(true);

        // UIの初期化
        gameUIManager.PlayerUIInitialize();

        // チュートリアル開始時にカーテンを開く
        CurtainManager.Instance.FullOpenAsync(GetType().Name).Forget();

        NetWorkAudioManager.Instance.PlayGlobal(BgmName.Tutorial);

        // チュートリアルの開始
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

    private void PlayerMoveStart()
    {
        PlayerOfflineController controller = PlayerUtility.GetPlayerByIndex(0).GetComponent<PlayerOfflineController>();

        tutorialGround.SetActive(false);

        controller.IsStop = false;
    }

    private void EndTutorial()
    {
        // チュートリアル終了時の処理をここに追加
        GameSceneManager.Instance.LoadLocalScene("Title").Forget();
    }
}

