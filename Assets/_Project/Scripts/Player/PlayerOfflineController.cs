using nira.Demo;
using R3;
using UnityEngine;
using UnityEngine.InputSystem.XInput;

[RequireComponent(typeof(PlayerRoot))]
public class PlayerOfflineController : MonoBehaviour, IPlayerActionHandler
{
    private PlayerRoot root;

    private PlayerInputController inputController;
    private NetworkPlayer player;

    public bool CanProcessInput
    {
        get
        {
            if (GameManager.Instance == null) return false;
            // ゲーム状態がPlaying かつ ダウンしていない時のみ入力を許可
            return GameManager.Instance.StateRx.CurrentValue == GameState.Playing && !root.IsDown.Value;
        }
    }

    private void Awake()
    {
        root = GetComponent<PlayerRoot>();
        player = GetComponent<NetworkPlayer>();

    }

    private void Start()
    {
        // --- UIの初期化 ---

        if (GameUIManager.Instance == null)
        {
            Debug.LogWarning("GameUIManager is not found in the scene.");
            return;
        }

        // UIManagerのインスタンスを取得
        GameUIManager uIManager = GameUIManager.Instance;

        // プレイヤーの名前と体力バーの初期化
        root.PlayerIndex.Value = 0;

        root.PlayerIndex.Where(idx => idx != -1).Take(1).Subscribe(idx =>
        {
            string playerName = PlayerDataManager.Instance.GetPlayerNameByIndex(idx);

            uIManager.SetPlayerName(idx, playerName);
            uIManager.SetHealthSliderMaxValue(idx, 100);
        }).AddTo(this);

        root.CurrentHealth.Subscribe(hp =>
        {
            if (root.PlayerIndex.Value != -1)
            {
                uIManager.UpdateHealth(root.PlayerIndex.Value, hp);
            }
        }).AddTo(this);

        if (PlayerUIManager.Instance != null && PlayerDataManager.Instance != null)
        {
            PlayerUIManager.Instance.Initialize(PlayerDataManager.Instance);
        }

        inputController = GetComponent<PlayerInputController>();

        root.Initialize(this, inputController);
    }


    // --- 各アクション処理 ---

    public void RequestAttack()
    {
        player.UseAttack(); // 攻撃のアクションを呼び出す

    }

    public void RequestSkillUse()
    {
        player.UseCurrentSkill(); // スキルのアクションを呼び出す
    }

}