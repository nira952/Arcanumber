using nira.Demo;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerRoot))]
public class PlayerNetworkController : NetworkBehaviour, IPlayerActionHandler
{
    private PlayerRoot root;

    // 自分の所有キャラ（IsOwner）であり、かつゲーム状態が Playing の時のみ入力を許可
    public bool CanProcessInput
    {
        get
        {
            if (!IsSpawned || !IsOwner) return false;
            if (GameManager.Instance == null) return false;

            // オンライン時は GameManager の CurrentState.Value を参照
            return GameManager.Instance.CurrentState.Value == GameState.Playing;
        }
    }

    private void Awake()
    {
        root = GetComponent<PlayerRoot>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;
        }

        root.Initialize(this);
    }

    // --- 各アクション処理（変更なし） ---
    public void RequestJump()
    {
        root.GetActionController().ExecuteJumpLocal();
        RequestJumpServerRpc();
    }

    [ServerRpc] private void RequestJumpServerRpc() => ExecuteJumpClientRpc();
    [ClientRpc]
    private void ExecuteJumpClientRpc()
    {
        if (IsOwner) return;
        root.GetActionController().ExecuteJumpLocal();
    }

    public void RequestAttack()
    {
        root.GetActionController().ExecuteAttackLocal();
        RequestAttackServerRpc();
    }

    [ServerRpc] private void RequestAttackServerRpc() => ExecuteAttackClientRpc();
    [ClientRpc]
    private void ExecuteAttackClientRpc()
    {
        if (IsOwner) return;
        root.GetActionController().ExecuteAttackLocal();
    }

    public void RequestSkillSelect(int direction)
    {
        root.GetActionController().ExecuteSkillSelectLocal(direction);
    }

    public void RequestSkillUse()
    {
        root.GetActionController().ExecuteSkillUseLocal();
        RequestSkillUseServerRpc();
    }

    [ServerRpc] private void RequestSkillUseServerRpc() => ExecuteSkillUseClientRpc();
    [ClientRpc]
    private void ExecuteSkillUseClientRpc()
    {
        if (IsOwner) return;
        root.GetActionController().ExecuteSkillUseLocal();
    }
}