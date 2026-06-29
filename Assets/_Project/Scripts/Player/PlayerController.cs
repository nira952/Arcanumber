using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの操作のクラス
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    private Rigidbody2D rb;
    private float moveInput;

    //操作の制限
    private bool isJump = false;    //ジャンプができるかどうか
    private bool isMove = false;    //移動できるかどうか
    private bool isChangeMove = false;  //移動の反転ができるかどうか
    private bool isNomalAttack = false; //通常攻撃が振れるか

    //エイム
    [SerializeField] private AimCursor aim;

    //アクションについて
    public event Action OnJumpEvent;    //ジャンプイベント

    public event Action OnAttackEvent;   //通常攻撃処理
    public event Action<int> OnSkillSelectEvent;    //スキル選択処理
    public event Action OnSkillUseEvent;    //スキル攻撃処理

    /// <summary>
    /// 初期設定
    /// </summary>
    public void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    /// <summary>
    /// ネットワークがつながった時の初期設定
    /// </summary>
    public void NetworkInitialize()
    {
        aim.Initialize();
    }

    /// <summary>
    /// 更新設定
    /// </summary>
    public bool PlayerPosUpdate()
    {
        if (rb != null) rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // 設置判定を確認する
        return IsGrounded();
    }

    /// <summary>
    /// アップデートを最後にする
    /// </summary>
    public void LatePlayerPosUpdate()
    {
        aim.AimUpdate();
    }

    /// <summary>
    /// 移動処理
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!isMove) return;

        moveInput = (!isChangeMove) ? context.ReadValue<float>() : -context.ReadValue<float>();
    }

    /// <summary>
    /// ジャンプボタンの処理
    /// </summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!isJump) return;

        if (context.performed) OnJumpEvent?.Invoke();
    }

    /// <summary>
    /// ジャンプリセット
    /// </summary>
    public bool IsGrounded()
    {
        if (groundCheck == null) return false;

        if (rb != null && rb.linearVelocity.y > 0.01f) return false;

        //足元の小さな円の範囲にgroundLayerがあるか判定
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    /// <summary>
    /// ジャンプ処理
    /// </summary>
    public void ExecuteJump()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    /// <summary>
    /// 通常攻撃処理
    /// </summary>
    public void Attack(InputAction.CallbackContext context)
    {
        if (!isNomalAttack) return;
        if (!context.performed) return;
        OnAttackEvent?.Invoke();
    }

    /// <summary>
    /// スキル選択処理
    /// </summary>
    public void OnSkillSelect(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Vector2 scrollValue = context.ReadValue<Vector2>();
        if (scrollValue.y > 0) OnSkillSelectEvent?.Invoke(1);
        else if (scrollValue.y < 0) OnSkillSelectEvent?.Invoke(-1);
    }

    /// <summary>
    /// スキル発動処理
    /// </summary>
    public void OnSkill(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        OnSkillUseEvent?.Invoke();
    }


    /**
     * --------- ゲッター ---------
     */
    public AimCursor GetAimCursor() { return aim; }

    /**
     * --------- セッター ---------
     */
    public void SetMoveSpeed(float speed) { this.moveSpeed = speed; }
    public void SetIsMove(bool isMove) 
    { 
        this.isMove = isMove;
        if (!this.isMove) moveInput = 0f;
    }
    public void SetIsJump(bool isJump) { this.isJump = isJump; }

    public void SetIsChangeMove(bool isChangeMove) { this.isChangeMove = isChangeMove; }
    public void SetIsNormalAttack(bool isNormalAttack) { this.isNomalAttack = isNormalAttack; }
}