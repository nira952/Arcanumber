using nira.Demo;
using R3;
using Unity.Services.Lobbies.Models;
using UnityEngine;

[RequireComponent(typeof(PlayerRoot))]
public class PlayerOfflineController : MonoBehaviour, IPlayerActionHandler
{
    private PlayerRoot root;

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

        PlayerInputController inputController = GetComponent<PlayerInputController>();

        root.Initialize(this, inputController);
    }

    // --- ダメージ処理の要求 ---
    public void RequestTakeDamage(int damage)
    {
        root.ApplyDamage(damage); // 直接処理
    }

    // --- 各アクション処理 ---
    public void RequestJump()
    {
        root.GetActionController().ExecuteJumpLocal(); // ローカルでジャンプ処理を実行
    }

    public void RequestAttack()
    {
        root.GetActionController().ExecuteAttackLocal();
        player.UseAttack(); // 攻撃のアクションを呼び出す

    }

    public void RequestSkillSelect(int direction) => root.GetActionController().ExecuteSkillSelectLocal(direction);

    public void RequestSkillUse()
    {
        player.UseCurrentSkill(); // スキルのアクションを呼び出す


    }
}