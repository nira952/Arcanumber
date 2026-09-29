using UnityEngine;

[RequireComponent(typeof(PlayerRoot))]
[RequireComponent(typeof(PlayerInputBinder))]
public class PlayerAutoController : MonoBehaviour, IPlayerInputMediator
{
    private PlayerRoot root;

    private void Start()
    {
        // --- コンポーネントの取得 ---
        root = GetComponent<PlayerRoot>();

        // --- 入力バインダーの初期化 ---

        // プレイヤーの初期化を実行
        root.Initialize();

        root.gameObject.tag = "Enemy";

    }


    // 入力をパススルーでRootに渡す

    public void OnAttackTriggered()
    {
        
    }

    // ローカルで移動入力を処理する
    public void OnMoveTriggered(float direction)
    {
        
    }

    public void OnJumpTriggered()
    {
        
    }

    public void OnSkillSelectTriggered(int skillIndex)
    {
        
    }

    public void OnSkillUseTriggered()
    {
        
    }

}