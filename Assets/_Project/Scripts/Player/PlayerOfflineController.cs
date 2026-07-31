using nira.Demo;
using R3;
using UnityEngine;

[RequireComponent(typeof(PlayerRoot))]
public class PlayerOfflineController : MonoBehaviour, IPlayerActionHandler
{
    private PlayerRoot root;

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
    }

    private void Start()
    {
        // オフライン用 UI更新処理（PlayerIndexは便宜上0など固定で割り当てる）
        root.PlayerIndex.Value = 0;

        root.PlayerIndex.Where(idx => idx != -1).Take(1).Subscribe(idx =>
        {
            if (GameUIManager.Instance != null)
            {
                string playerName = PlayerDataManager.Instance.GetPlayerNameByIndex(idx);


                GameUIManager.Instance.SetPlayerName(idx, playerName);
                GameUIManager.Instance.SetHealthSliderMaxValue(idx, 100);
            }
        }).AddTo(this);

        root.CurrentHealth.Subscribe(hp => {
            if (root.PlayerIndex.Value != -1 && GameUIManager.Instance != null)
            {
                GameUIManager.Instance.UpdateHealth(root.PlayerIndex.Value, hp);
            }
        }).AddTo(this);
    }

    // オフライン時は自分自身に直接ダメージ処理を行う
    public void RequestTakeDamage(int damage)
    {
        root.ApplyDamage(damage);
    }

    public void RequestJump() => root.GetActionController().ExecuteJumpLocal();
    public void RequestAttack() => root.GetActionController().ExecuteAttackLocal();
    public void RequestSkillSelect(int direction) => root.GetActionController().ExecuteSkillSelectLocal(direction);
    public void RequestSkillUse() => root.GetActionController().ExecuteSkillUseLocal();
}