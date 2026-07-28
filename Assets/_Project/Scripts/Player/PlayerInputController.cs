using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using R3;

/// <summary>
/// 入力を検知し、R3のストリームとして発行するクラス
/// </summary>
public class PlayerInputController : NetworkBehaviour
{
    private readonly Subject<float> moveSubject = new();
    public Observable<float> OnMoveAsObservable => moveSubject;

    private readonly Subject<Unit> jumpSubject = new();
    public Observable<Unit> OnJumpAsObservable => jumpSubject;

    private readonly Subject<Unit> attackSubject = new();
    public Observable<Unit> OnAttackAsObservable => attackSubject;

    private readonly Subject<int> skillSelectSubject = new();
    public Observable<int> OnSkillSelectAsObservable => skillSelectSubject;

    private readonly Subject<Unit> skillUseSubject = new();
    public Observable<Unit> OnSkillUseAsObservable => skillUseSubject;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // 自分のキャラクター以外は、入力コンポーネント自体を無効化して完全に沈黙させる
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Debug.Log($"[PlayerInputController] OnMove called with value: {context.ReadValue<float>()}");
        moveSubject.OnNext(context.ReadValue<float>());
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) jumpSubject.OnNext(Unit.Default);
    }

    public void Attack(InputAction.CallbackContext context)
    {
        if (context.performed) attackSubject.OnNext(Unit.Default);
    }

    public void OnSkillSelect(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Vector2 scrollValue = context.ReadValue<Vector2>();
        if (scrollValue.y > 0) skillSelectSubject.OnNext(1);
        else if (scrollValue.y < 0) skillSelectSubject.OnNext(-1);
    }

    public void OnSkill(InputAction.CallbackContext context)
    {
        if (context.performed) skillUseSubject.OnNext(Unit.Default);
    }

    private void OnDestroy()
    {
        moveSubject.Dispose();
        jumpSubject.Dispose();
        attackSubject.Dispose();
        skillSelectSubject.Dispose();
        skillUseSubject.Dispose();
    }
}