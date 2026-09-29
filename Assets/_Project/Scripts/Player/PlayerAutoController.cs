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

        root.gameObject.tag = "Enemy";

    }

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