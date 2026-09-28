using R3;
using UnityEngine;

[RequireComponent(typeof(PlayerRoot))]
public class PlayerOfflineController : MonoBehaviour, IPlayerInputMediator
{
    private PlayerRoot root;

    private PlayerInputController inputController;
    private PlayerRoot player;

    public bool CanProcessInput
    {
        get
        {
            if (GameManager.Instance == null) return false;
            // ゲーム状態がPlaying かつ ダウンしていない時のみ入力を許可
            return GameManager.Instance.StateRx.CurrentValue == GameState.Playing && !root.IsDown.Value;
        }
    }

    public void OnAttackTriggered()
    {
        throw new System.NotImplementedException();
    }

    public void OnJumpTriggered()
    {
        throw new System.NotImplementedException();
    }

    public void OnMoveTriggered(float direction)
    {
        throw new System.NotImplementedException();
    }

    public void OnSkillSelectTriggered(int skillIndex)
    {
        throw new System.NotImplementedException();
    }

    public void OnSkillUseTriggered()
    {
        throw new System.NotImplementedException();
    }

    private void Awake()
    {
        root = GetComponent<PlayerRoot>();
        player = GetComponent<PlayerRoot>();

    }

    private void Start()
    {
        //// --- UIの初期化 ---

        //if (GameUIManager.Instance == null)
        //{
        //    Debug.LogWarning("GameUIManager is not found in the scene.");
        //    return;
        //}
        //GameCameraManager.Instance.RegisterTarget(this.transform); // カメラにプレイヤーを登録

        //// UIManagerのインスタンスを取得
        //GameUIManager uIManager = GameUIManager.Instance;

        // プレイヤーの名前と体力バーの初期化
        //root.PlayerIndex.Value = 0;

        //root.PlayerIndex.Where(idx => idx != -1).Take(1).Subscribe(idx =>
        //{
        //    string playerName = PlayerDataManager.Instance.GetPlayerNameByIndex(idx);

        //    uIManager.SetPlayerName(idx, playerName);
        //    uIManager.SetHealthSliderMaxValue(idx, 100);
        //}).AddTo(this);

        //root.CurrentHealth.Subscribe(hp =>
        //{
        //    if (root.PlayerIndex.Value != -1)
        //    {
        //        uIManager.UpdateHealth(root.PlayerIndex.Value, hp);
        //    }
        //}).AddTo(this);

        //if (PlayerUIManager.Instance != null && PlayerDataManager.Instance != null)
        //{
        //    PlayerUIManager.Instance.Initialize(PlayerDataManager.Instance);
        //}

        //inputController = GetComponent<PlayerInputController>();

        //root.Initialize(this, inputController);
    }


}