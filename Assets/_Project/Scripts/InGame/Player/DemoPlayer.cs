using R3;
using UnityEngine;

namespace nira.Demo
{
    public class DemoPlayer : MonoBehaviour
    {
        // R3のReactivePropertyに変更
        public SerializableReactiveProperty<int> PlayerIndex = new(-1);
        public SerializableReactiveProperty<int> CurrentHealth = new(100);
        public SerializableReactiveProperty<bool> IsDown = new(false);

        private readonly int maxHealth = 100;
        private IPlayerActionHandler actionHandler;

        [SerializeField] private DemoSkill skill;

        private void Awake()
        {
            actionHandler = GetComponent<IPlayerActionHandler>();
        }

        private void Update()
        {
            // 入力処理は、オンライン/オフライン共通化のため、IPlayerActionHandlerを通して委譲する
            if (actionHandler == null || !actionHandler.CanProcessInput) return;

            // 移動処理：オン/オフライン共通なのでインターフェースを通さずその場で処理！
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                // 自身のコンポーネント（CharacterController等）を動かす
                // オンラインならNetworkTransformがこれを自動検知して同期する
                //MoveLocal(Vector3.forward);
            }

            // ダメージ処理：オンライン時はRpc通信が必要なので、インターフェースに委譲する
            if (Input.GetKeyDown(KeyCode.Space))
            {
                actionHandler.RequestTakeDamage(10);
            }
        }
        /// <summary>
        /// 【ローカル/サーバー共通】純粋に自分の値を減らすロジック
        /// </summary>
        public void ApplyDamage(int damage)
        {
            if (IsDown.Value) return;

            CurrentHealth.Value -= damage;
            Debug.Log($"Player {PlayerIndex.Value} took {damage} damage. Current health: {CurrentHealth.Value}");

            if (CurrentHealth.Value <= 0)
            {
                CurrentHealth.Value = 0;
                IsDown.Value = true;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckFinishCondition();
                }
            }
        }

        public void Initialize(int index)
        {
            PlayerIndex.Value = index;
            CurrentHealth.Value = maxHealth;
        }
    }
}