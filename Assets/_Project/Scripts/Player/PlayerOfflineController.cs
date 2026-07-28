using nira.Demo;
using UnityEngine;

[RequireComponent(typeof(PlayerRoot))]
public class PlayerOfflineController : MonoBehaviour, IPlayerActionHandler
{
    private PlayerRoot root;

    // ゲーム状態が Playing の時のみ入力を許可する
    public bool CanProcessInput
    {
        get
        {
            if (GameManager.Instance == null) return false;

            // StateRx の値が Playing かどうかをチェック
            bool isPlaying = GameManager.Instance.StateRx.CurrentValue == GameState.Playing;

            // HPなどのダウン判定を追加する場合はここで && !player.IsDown.Value を付与
            return isPlaying;
        }
    }

    private void Awake()
    {
        root = GetComponent<PlayerRoot>();
    }

    private void Start()
    {
        root.Initialize(this);

        if (GameManager.Instance != null) GameManager.Instance.RegisterPlayer(root);

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.SetPlayerName(0, "練習用プレイヤー");
            GameUIManager.Instance.SetHealthSliderMaxValue(0, 100);
        }

    }

    public void RequestJump() => root.GetActionController().ExecuteJumpLocal();
    public void RequestAttack() => root.GetActionController().ExecuteAttackLocal();
    public void RequestSkillSelect(int direction) => root.GetActionController().ExecuteSkillSelectLocal(direction);
    public void RequestSkillUse() => root.GetActionController().ExecuteSkillUseLocal();
}