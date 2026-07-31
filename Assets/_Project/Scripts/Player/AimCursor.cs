using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// スキルの標準設定
/// </summary>
public class AimCursor : NetworkBehaviour
{
    [SerializeField] private GameObject aimCursor;  //標準の位置
    [SerializeField] private Transform normalPos;   //プレイヤーの位置（中心）
    [SerializeField] private Transform efeUpperPos;    //エフェクトの位置

    [SerializeField] private GameObject magicStart;

    //private Camera _mainCam;   //カメラの位置
    private Vector3 _mouseWorldPos; //現在のマウスの位置

    private AimSelect _currentMode; //どの標準方法か

    //計算で使うパラメータ
    private float _currentRadius = 0f;  //半径
    private bool _useLerp = true;   //移動を滑らかにするか
    private bool _useRadiusLimit = false;   //円周にするか
    private Transform _lockOnTarget = null; //ロックオンにするか

    private void Start()
    {
        if (!IsOwner) gameObject.SetActive(false);
    }

    /// <summary>
    /// 初期設定
    /// </summary>
    public void Initialize()
    {
        // システムカーソルを非表示に
        Cursor.visible = false;
        // 初期状態としてカーソルを表示する
        if (aimCursor != null)
            aimCursor.SetActive(true);
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    public void AimUpdate()
    {
        if (aimCursor == null) return;
        //マウス位置を更新
        UpdateMousePosition();
        //ロックオンモードの場合のみ、毎フレーム一番近い敵を追いかける
        Vector3? targetPos = null;
        if (_currentMode == AimSelect.LookOn)
        {
            _lockOnTarget = GetNearestEnemyOnScreen();
            if (_lockOnTarget != null)
            {
                targetPos = _lockOnTarget.position;
            }
        }
        //決定されたパラメータを元に、カーソルを実際に移動させる（毎フレーム実行）
        ApplyAimMovement(_currentRadius, _useLerp, _useRadiusLimit, targetPos);
    }

    /// <summary>
    /// どの攻撃範囲か選択するメソッド
    /// </summary>
    public void SelectAim(AimSelect aim)
    {
        aimCursor.SetActive(true);
        _currentMode = aim;
        // パラメータの初期化
        _currentRadius = 0f;
        _useLerp = true;
        _useRadiusLimit = false;
        _lockOnTarget = null;
        switch (aim)
        {
            case AimSelect.Direction:
                _currentRadius = GameConfig.PROX_RADIUS;
                _useLerp = false;
                _useRadiusLimit = true;
                break;
            case AimSelect.AutoFollow:
                // デフォルト値のまま
                break;
            case AimSelect.LookOn:
                _lockOnTarget = GetNearestEnemyOnScreen();
                _useLerp = false;
                break;
            case AimSelect.None:
                aimCursor.SetActive(false);
                break;
        }
    }

    /// <summary>
    /// 照準を計算・移動させる
    /// </summary>
    private void ApplyAimMovement(float radius, bool useLerp, bool useRadiusLimit, Vector3? targetPos)
    {
        //ターゲット位置を取得
        Vector3 baseTarget = targetPos ?? _mouseWorldPos;
        Vector3 goalPos;

        //プレイヤーの位置を中心点にする
        Vector3 centerPosition = normalPos != null ? normalPos.position : transform.position;
        centerPosition.z = 0;   //2D

        if (useRadiusLimit)
        {
            //指定された中心点からターゲットへのベクトル計算
            Vector3 toTarget = baseTarget - centerPosition;
            toTarget.z = 0;
            //ゼロ除算の対策
            if (toTarget.sqrMagnitude < 0.001f)
                toTarget = Vector3.right;
            //向きだけを抽出
            Vector3 dir = toTarget.normalized;
            //指定したTransformの位置を中心に、向き×半径の場所計算
            goalPos = centerPosition + dir * radius;
        }
        else
            goalPos = baseTarget;

        //全ての計算が終わった「最終的な目標座標」を画面内に収める！
        goalPos = ClampPositionToScreen(goalPos);

        //移動の適用
        if (useLerp)
            aimCursor.transform.position = Vector3.Lerp(aimCursor.transform.position, goalPos, Time.deltaTime * GameConfig.FLLOW_SPEED);
        else
            aimCursor.transform.position = goalPos;
    }

    /// <summary>
    /// 画面内でマウスに最も近いEnemyタグのTransformを返す
    /// </summary>
    private Transform GetNearestEnemyOnScreen()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float minDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            // 画面内判定
            Vector3 viewPos = Camera.main.WorldToViewportPoint(enemy.transform.position);
            if (viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1)
            {
                float dist = Vector2.Distance(_mouseWorldPos, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = enemy.transform;
                }
            }
        }
        return nearest;
    }

    /// <summary>
    /// ロックオン中ならその相手を、そうでないなら自分以外のプレイヤーを返す
    /// </summary>
    public NetworkPlayer GetLockOnNetworkPlayer()
    {
        if (_currentMode != AimSelect.LookOn || _lockOnTarget == null)
            return null;

        if (_lockOnTarget.TryGetComponent(out NetworkPlayer targetPlayer))
            return targetPlayer;
        return null;
    }

    /// <summary>
    /// マウスのポジションを更新するメソッド
    /// </summary>
    private void UpdateMousePosition()
    {

        Vector3 screenPos = Mouse.current.position.ReadValue();
        _mouseWorldPos = Camera.main.ScreenToWorldPoint(screenPos);
        _mouseWorldPos.z = 0;
    }

    /// <summary>
    /// 指定された座標を画面の枠内（5%〜95%）に収めて返すメソッド
    /// </summary>
    private Vector3 ClampPositionToScreen(Vector3 targetPos)
    {
        Vector3 viewPos = Camera.main.WorldToViewportPoint(targetPos);
        viewPos.x = Mathf.Clamp(viewPos.x, 0.05f, 0.95f);
        viewPos.y = Mathf.Clamp(viewPos.y, 0.05f, 0.95f);

        Vector3 clampedWorldPos = Camera.main.ViewportToWorldPoint(viewPos);
        clampedWorldPos.z = 0;
        return clampedWorldPos;
    }

    /**
     * --------- ゲッター ---------
     */
    public Transform GetTransform() { return aimCursor.transform; }
    public Transform GetEfeUpperPos() { return efeUpperPos; }
}


/// <summary>
/// 攻撃のエイムの種類
/// </summary>
public enum AimSelect
{
    [InspectorName("方向選択")]Direction,
    [InspectorName("ホーミング")] AutoFollow,
    [InspectorName("ターゲット指定")] LookOn,
    [InspectorName("ターゲット指定なし")] LookOff,
    [InspectorName("標準無し")] None
}