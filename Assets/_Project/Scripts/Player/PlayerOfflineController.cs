using UnityEngine;

[RequireComponent(typeof(PlayerRoot))]
[RequireComponent(typeof(PlayerInputBinder))]
public class PlayerOfflineController : MonoBehaviour, IPlayerInputMediator
{
    private PlayerRoot root;
    private PlayerInputBinder inputBinder;

    private void Start()
    {
        // --- コンポーネントの取得 ---
        root = GetComponent<PlayerRoot>();            // メインスクリプトを取得
        inputBinder = GetComponent<PlayerInputBinder>();     // 入力バインダーの取得


        // バインダーに現在のコントローラーを設定し、入力イベントを購読する
        inputBinder.Initialize(this);

        // プレイヤーの初期化を実行
        root.Initialize();
    }


    // 入力をパススルーでRootに渡す

    public void OnAttackTriggered()
    {
        // 攻撃を実行する
        root.ExecuteAttack();
    }

    // ローカルで移動入力を処理する
    public void OnMoveTriggered(float direction)
    {
        root.ExecuteMove(direction); // 移動のアクションを呼び出す
    }

    public void OnJumpTriggered()
    {
        root.ExecuteJump();
    }

    public void OnSkillSelectTriggered(int skillIndex)
    {
        root.ExecuteSkillSelect(skillIndex);
    }

    public void OnSkillUseTriggered()
    {
        root.ExecuteSkillUse();
    }

}