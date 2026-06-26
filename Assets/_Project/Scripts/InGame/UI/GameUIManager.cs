using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace nira.Demo
{
    public class GameUIManager : MonoBehaviour
    {
        #region Singleton Pattern
        public static GameUIManager Instance { get; private set; }

        private static readonly Subject<Unit> onInitialized = new();
        public static Observable<Unit> OnInitialized => onInitialized;

        private void SetUpSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                onInitialized.OnNext(Unit.Default);
            }
            else
            {
                Destroy(gameObject);
            }
            Debug.Log("[GameUIManager] Awake called. Instance set.");

        }

        #endregion

        [SerializeField] private Slider[] healthSliders = new Slider[4];
        [SerializeField] private TextMeshProUGUI[] playerNameTexts = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI timerText;

        private void Awake()
        {
            SetUpSingleton(); // シングルトンの初期化

            // 初期化時に全てのスライダーを非表示にする
            foreach (var slider in healthSliders)
            {
                slider.gameObject.SetActive(false);
            }

            // 初期化時に全てのプレイヤー名テキストを非表示にする
            foreach (var text in playerNameTexts)
            {
                text.gameObject.SetActive(false);
            }

            if (timerText != null)
            {
                timerText.text = "";
            }
        }

        /// <summary>
        /// 指定したプレイヤーの名前テキストを設定する
        /// </summary>
        public void SetPlayerName(int playerIndex, string playerName)
        {
            if (playerIndex < 0 || playerIndex >= playerNameTexts.Length) return;

            playerNameTexts[playerIndex].gameObject.SetActive(true);
            playerNameTexts[playerIndex].text = playerName;
        }

        /// <summary>
        /// 指定したプレイヤーの体力スライダーの最大値を設定する
        /// </summary>
        public void SetHealthSliderMaxValue(int playerIndex, int maxHealth)
        {
            if (playerIndex < 0 || playerIndex >= healthSliders.Length) return;

            healthSliders[playerIndex].gameObject.SetActive(true);
            healthSliders[playerIndex].maxValue = maxHealth;
            healthSliders[playerIndex].value = maxHealth; // 初期値を満タンに
        }

        /// <summary>
        /// 指定したプレイヤーの体力スライダーの値を更新する
        /// </summary>
        public void UpdateHealth(int playerIndex, int health)
        {
            if (playerIndex < 0 || playerIndex >= healthSliders.Length) return;
            healthSliders[playerIndex].value = health;
        }

        /// <summary>
        /// タイマーの表示を更新する
        /// </summary>
        public void UpdateTimer(string time)
        {
            if (timerText != null)
            {
                timerText.text = time;
            }
        }
    }
}