using UnityEngine;

[RequireComponent(typeof(PlayerRoot))]
[RequireComponent(typeof(PlayerInputBinder))]
public class PlayerOfflineController : MonoBehaviour, IPlayerInputMediator
{
    private PlayerRoot root;
    private PlayerInputBinder inputBinder;

    public bool IsStop = false;

    private void Awake()
    {
        // --- コンポーネントの取得 ---
        root = GetComponent<PlayerRoot>();            // メインスクリプトを取得
        inputBinder = GetComponent<PlayerInputBinder>();     // 入力バインダーの取得
        PlayerUtility.RegisterPlayer(root);
        GameCameraManager.Instance.RegisterTarget(root.transform);

        // バインダーに現在のコントローラーを設定し、入力イベントを購読する
        inputBinder.Initialize(this);


    }

    private void Start()
    {

        // プレイヤーの初期化を実行
        root.OwnerInitialize();

        SkillManager.Instance.SetSkillList(root.PlayerIndex.Value, root.GetSkill());

    }


    // 入力をパススルーでRootに渡す

    public void OnAttackTriggered()
    {
        if (IsStop) { return; }

        // 攻撃を実行する
        root.ExecuteAttack();
    }

    // ローカルで移動入力を処理する
    public void OnMoveTriggered(float direction)
    {
        if (IsStop) { return; }

        root.ExecuteMove(direction); // 移動のアクションを呼び出す
    }

    public void OnJumpTriggered()
    {
        if (IsStop) { return; }

        root.ExecuteJump();
    }

    public void OnSkillSelectTriggered(int skillIndex)
    {
        if (IsStop) { return; }

        root.ExecuteSkillSelect(skillIndex);
    }

    public void OnSkillUseTriggered()
    {
        if (IsStop) { return; }

        root.ExecuteSkillUse();
    }

}