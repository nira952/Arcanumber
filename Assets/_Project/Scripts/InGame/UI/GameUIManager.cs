using DG.Tweening;
using ObservableCollections;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Serializable]
    private class PlayerEffectBlock
    {
        // 事前にインスペクターで5枚程度の Image をセットしておく
        public List<Image> availableImages = new List<Image>();

        public void HideAll()
        {
            foreach (var img in availableImages)
            {
                if (img != null) { img.gameObject.SetActive(false); }
            }
        }
    }

    [SerializeField] private TextMeshProUGUI timerText;

    [SerializeField] private TextMeshProUGUI gameStateText;

    [SerializeField] private TextMeshProUGUI powerUpText;

    [Header("ステータス関係 (プレイヤーごと)")]
    [SerializeField] private PlayerEffectBlock[] playerEffectBlocks = new PlayerEffectBlock[4];

    [SerializeField] private GameObject[] statusObjects = new GameObject[4]; // プレイヤーごとのステータスUIオブジェクトs

    [SerializeField] private Slider[] healthSliders = new Slider[4];
    [SerializeField] private TextMeshProUGUI[] hpText;
    [SerializeField] private TextMeshProUGUI[] playerNameTexts = new TextMeshProUGUI[4];

    [Header("リザルト関連")]
    [SerializeField] private CanvasGroup resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    public Button endButton;
    public Button reMatchButton;

    private readonly CompositeDisposable playerSubscriptions = new();

    public void Initialize(GameManager gameManager, TimeManager timeManager)
    {
        // --- 1. CompositeDisposable の初期化（nullチェック） ---
        if (playerSubscriptions == null)
        {
            Debug.LogError("[GameUIManager] playerSubscriptions が null です！");
            return;
        }
        else
        {
            playerSubscriptions.Clear(); // 既存の購読をクリア
        }

        Debug.Log("[GameUIManager] Initialize called.1");

        // --- 2. GameManager の Subscribe (StateRxのnullチェック) ---
        if (gameManager != null && gameManager.StateRx != null)
        {
            gameManager.StateRx.Subscribe(state =>
            {
                switch (state)
                {
                    case GameState.Playing:
                        UpdateGameStateText("Start!");
                        break;
                    case GameState.Finish:
                        UpdateGameStateText("Finish");
                        break;
                }
            }).AddTo(this);
        }
        else
        {
            Debug.LogError("[GameUIManager] gameManager または StateRx が null です！");
        }

        Debug.Log("[GameUIManager] Initialize called.2");

        // タイマーの初期表示
        if (timerText != null) { timerText.text = ""; }


        // --- 4. TimeManager の Subscribe ---
        if (timeManager != null && timeManager.RemainingTime != null)
        {
            timeManager.RemainingTime.Subscribe(time =>
            {
                UpdateTimerDisplay((int)time);
            }).AddTo(this);
        }

        PlayerUIInitialize();
    }

    public void PlayerUIInitialize()
    {
        // --- 3. playerEffectBlocks の null チェック ---
        if (playerEffectBlocks != null)
        {
            foreach (var block in playerEffectBlocks)
            {
                if (block != null) block.HideAll();
            }
        }

        // ステータスオブジェクトの非表示
        if (statusObjects != null)
        {
            for (int i = 0; i < statusObjects.Length; i++)
            {
                if (statusObjects[i] != null) statusObjects[i].SetActive(false);
            }
        }

        // --- 5. プレイヤーリストの処理 ---
        List<PlayerRoot> playerList = PlayerUtility.GetAllPlayer();
        if (playerList == null) { return; }
        foreach (var root in playerList)
        {
            Debug.Log($"[GameUIManager] PlayerRoot found: {root?.name}, PlayerIndex: {root?.PlayerIndex.Value}");

            if (root == null || root.PlayerIndex.Value < 0) continue;

            int pIndex = root.PlayerIndex.Value;

            // PlayerDataManager の null チェック
            string playerName = "Player";
            if (PlayerDataManager.Instance != null)
            {
                playerName = PlayerDataManager.Instance.GetPlayerNameByIndex(pIndex);
            }

            Debug.Log($"[GameUIManager] PlayerIndex: {pIndex}, Name: {playerName}");

            // プレイヤー名の初期表示と購読
            SetPlayerName(pIndex, playerName);
            root.PlayerName.Subscribe(name =>
            {
                SetPlayerName(pIndex, name);
            }).AddTo(playerSubscriptions);


            // 体力スライダーの最大値を設定し、初期値を更新
            SetHealthSliderMaxValue(pIndex, 100);
            UpdateHealth(pIndex, root.CurrentHealth.Value);
            root.CurrentHealth.Subscribe(hp =>
            {
                UpdateHealth(pIndex, hp);
            }).AddTo(playerSubscriptions);


            // アクティブ効果の変更購読
            if (root.ActiveEffects != null)
            {
                root.ActiveEffects.ObserveChanged()
                    .Subscribe(_ =>
                    {
                        UpdateActiveEffects(pIndex, root.ActiveEffects.ToList());
                    }).AddTo(playerSubscriptions);
            }
        }

    }


    /// <summary>
    /// 指定したプレイヤーの名前テキストを設定する
    /// </summary>
    private void SetPlayerName(int playerIndex, string playerName)
    {
        if (playerIndex < 0 || playerIndex >= playerNameTexts.Length) return;

        playerNameTexts[playerIndex].gameObject.SetActive(true);
        playerNameTexts[playerIndex].text = playerName;
    }

    /// <summary>
    /// 指定したプレイヤーの体力スライダーの最大値を設定する
    /// </summary>
    private void SetHealthSliderMaxValue(int playerIndex, float maxHealth)
    {
        if (playerIndex < 0 || playerIndex >= healthSliders.Length) return;

        statusObjects[playerIndex].SetActive(true);

        healthSliders[playerIndex].gameObject.SetActive(true);
        healthSliders[playerIndex].maxValue = maxHealth;
        healthSliders[playerIndex].value = maxHealth; // 初期値を満タンに
    }

    /// <summary>
    /// 指定したプレイヤーの体力スライダーの値を更新する
    /// </summary>
    private void UpdateHealth(int playerIndex, float health)
    {
        if (playerIndex < 0 || playerIndex >= healthSliders.Length) return;
        healthSliders[playerIndex].value = health;
        if (hpText != null && playerIndex < hpText.Length)
        {
            // 小数点以下を切り捨てて整数表示にする
            hpText[playerIndex].text = health.ToString("F0");
        }
    }

    public void UpdateGameStateText(string text)
    {
        if (gameStateText != null)
        {
            // alphaを1にして表示する
            gameStateText.DOFade(1f, 0f);

            gameStateText.text = text;
        }
    }

    public void HideGameStateText()
    {
        // Dotweenでフェードアウトさせる
        if (gameStateText != null)
        {
            gameStateText.DOFade(0f, 0.5f).OnComplete(() =>
            {
                gameStateText.text = "";
                gameStateText.DOFade(1f, 0f); // フェードアウト後に透明度を元に戻す
            });
        }
    }

    // UIの表示フォーマット更新 (mm:ss)
    private void UpdateTimerDisplay(int totalSeconds)
    {
        if (timerText == null) return;

        TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
        // mm:ss で「05:00」のような2桁固定表記になります
        timerText.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }



    // バフ・デバフの表示をプレイヤーごとにバインドする処理
    /// <summary>
    /// エフェクト表示の更新処理
    /// </summary>
    private void UpdateActiveEffects(int playerIndex, List<EffectAbility> effects)
    {
        Debug.Log($"[GameUIManager] エフェクトを更新: {playerIndex}");

        var targetBlock = playerEffectBlocks[playerIndex];

        // 1. まず用意されているイメージ枠を全て非表示にする
        targetBlock.HideAll();

        if (effects == null || effects.Count == 0) return;

        // 次に割り当てる Image のインデックス
        int imageIndex = 0;

        // 2. アクティブなエフェクトを空いている Image 枠へ左から順にセット
        foreach (var effect in effects)
        {
            if (effect == null) continue;

            // 表示対象でない、または画像が設定されていない場合はスキップ（表示枠を消費しない）
            if (!effect.IsDisplay()) continue;

            var effectData = effect.GetEffect();
            if (effectData == null) continue;

            var sprite = effectData.GetEffectImage();
            if (sprite == null) continue;

            // 用意してある Image 枠の枚数を超えたらそれ以上は表示しない（溢れ防止）
            if (imageIndex >= targetBlock.availableImages.Count)
            {
                Debug.LogWarning($"[StatusUIManager] 表示上限（{targetBlock.availableImages.Count}個）を超えたため一部バフを非表示にしました。");
                break;
            }

            // 使用していない Image を取得して適用
            Image targetImage = targetBlock.availableImages[imageIndex];
            if (targetImage != null)
            {
                targetImage.sprite = sprite;
                targetImage.gameObject.SetActive(true);
                imageIndex++; // 次の枠へインクリメント
            }
        }
    }


    public void ShowPowerUpText()
    {
        // PowerUpTextをフェードで1秒間表示し、その後フェードアウトする
        powerUpText.text = "残り30秒 全員の攻撃力増加！";

        if (powerUpText != null)
        {
            powerUpText.DOFade(1f, 0.5f).OnComplete(() =>
            {
                powerUpText.DOFade(0f, 1f).SetDelay(2f);
            });
        }
    }

    public void ShowResult(string resultMessage)
    {
        if (resultPanel != null && resultText != null)
        {
            resultText.text = resultMessage;
            resultPanel.alpha = 1f;
            resultPanel.interactable = true;
            resultPanel.blocksRaycasts = true;
        }
    }

    public void ShowEndButton()
    {
        endButton.gameObject.SetActive(true);
        reMatchButton.gameObject.SetActive(true);


    }


    private void OnDestroy()
    {
        playerSubscriptions.Dispose();
    }
}

