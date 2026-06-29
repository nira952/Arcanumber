using UnityEngine;

public class AIPlayer : MonoBehaviour
{
    private enum AIState { IDLE, MOVE, JUMP }
    private AIState currentState = AIState.IDLE;    //現在の状態

    private float moveSpeed = 5f;  //移動速度
    private float jumpForce = 12f;  //ジャンプの威力

    [SerializeField] private Rigidbody2D rb; //リジッドボディ
    private float stateTimer = 0;   //継続時間
    private int moveDirection = 1;  //移動方向

    //接地判定用
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    NetworkPlayer enemy;

    private bool isAIPlayer = false;

    void Start()
    {
        enemy = GetComponent<NetworkPlayer>();
        // 最初の状態をセット
        ChangeState(AIState.IDLE);
    }

    private void Update()
    {
        //動くべきではなかったら動かない
        if (!isAIPlayer)
        {
            rb.linearVelocity = Vector2.zero;
            ChangeState(AIState.IDLE);
        }

        TimerCount();

        switch (currentState)
        {
            case AIState.IDLE: UpdateIdle(); break;
            case AIState.MOVE: UpdateMove(); break;
            case AIState.JUMP: UpdateJump(); break;
        }
    }

    /// <summary>
    /// 停止時のメソッド
    /// </summary>
    private void UpdateIdle()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (stateTimer <= 0) ChangeState(AIState.MOVE);
    }

    /// <summary>
    /// 移動時のメソッド
    /// </summary>
    private void UpdateMove()
    {
        if (!IsMove()) return;

        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        if (IsGrounded() && Random.value < 0.01f)
            ChangeState(AIState.JUMP);
        else if (stateTimer <= 0)
            ChangeState(AIState.IDLE);
    }

    /// <summary>
    /// ジャンプ時のメソッド
    /// </summary>
    private void UpdateJump()
    {
        if(!IsJump()) return;
        if (IsGrounded())
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        ChangeState(AIState.MOVE);
    }

    /// <summary>
    /// 時間を減らすメソッド
    /// </summary>
    void TimerCount() { stateTimer-= Time.deltaTime; }

    /// <summary>
    /// 地面についているかを確認するメソッド
    /// </summary>
    private bool IsGrounded()
    {
        if (groundCheck == null) return true;
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    /// <summary>
    /// AIの状態を変更するメソッド
    /// </summary>
    private void ChangeState(AIState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case AIState.IDLE: EnterIdle(); break;
            case AIState.MOVE: EnterMove(); break;
            case AIState.JUMP: break;
        }
    }

    /// <summary>
    /// 止まっている時間を決めるメソッド
    /// </summary>
    private void EnterIdle()
    {
        stateTimer = Random.Range(0.5f, 1.5f);
    }

    /// <summary>
    /// 移動している時間を決めるメソッド
    /// </summary>
    private void EnterMove()
    {
        stateTimer = Random.Range(0.5f, 1.5f);

        // 稀に方向転換するロジック
        if (Random.value < 0.3f)
            moveDirection *= -1;
        else
            moveDirection = Random.Range(0, 2) == 0 ? 1 : -1;
    }

    /// <summary>
    /// 歩くことができる条件
    /// </summary>
    private bool IsMove()
    {
        return !PlayerUtility.HaveEffect(enemy, EffectList.Stun, false);
    }

    /// <summary>
    /// ジャンプできる条件
    /// </summary>
    private bool IsJump()
    {
        return !(PlayerUtility.HaveEffect(enemy, EffectList.Stun, false)
            || PlayerUtility.HaveEffect(enemy, EffectList.NoJump, false));
    }


    public bool GetIsAIPlayer() => isAIPlayer;
    public void SetIsAIPlayer(bool aIPlayer) { isAIPlayer =  aIPlayer; }
}
