using R3;
using UnityEngine;

namespace nira.Demo
{
    [RequireComponent(typeof(DemoPlayer))]
    public class PlayerOfflineController : MonoBehaviour, IPlayerActionHandler
    {
        private DemoPlayer player;

        public bool CanProcessInput => !player.IsDown.Value;

        private void Awake()
        {
            player = GetComponent<DemoPlayer>();
        }

        private void Start()
        {
            // オフライン用の初期化（ID 0 としてUIをセットアップ）
            player.Initialize(0);

            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.SetPlayerName(0, "練習用プレイヤー");
                GameUIManager.Instance.SetHealthSliderMaxValue(0, 100);
            }

            // オフライン時のUI更新購読
            player.CurrentHealth.Subscribe(hp =>
            {
                if (GameUIManager.Instance != null) GameUIManager.Instance.UpdateHealth(0, hp);
            }).AddTo(this);
        }

        public void RequestTakeDamage(int damage)
        {
            // オフラインなので直接自分のデータを減らすだけ
            player.ApplyDamage(damage);
        }

        public void RequestActivateSkill()
        {
            throw new System.NotImplementedException();
        }
    }
}