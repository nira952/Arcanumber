using DG.Tweening;
using NPOI.SS.Formula.Functions;
using ObservableCollections;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager :MonoBehaviour
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

    public void Initialize(GameManager gameManager,TimeManager timeManager)
    {
        // --- ストリームを購読してUIを更新する処理 ---

        // ゲーム状態の変更を購読し、UI更新とログ出力を行う
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

        // タイマーの初期表示を空にする
        if (timerText != null) { timerText.text = ""; }

        // エフェクトを全て非表示に
        foreach (var block in playerEffectBlocks)
        {
            block.HideAll();
        }

        // タイマーの残り時間を購読し、UIのタイマー表示を更新する
        timeManager.RemainingTime.Subscribe(time =>
        {
            UpdateTimerDisplay((int)time);
        }).AddTo(this);


        // 一度すべてのプレイヤーのステータスを非表示にしておく
        for (int i = 0; i < statusObjects.Length; i++)
        {
            if (statusObjects[i] != null)
            {
                statusObjects[i].SetActive(false);
            }
        }

        List<PlayerRoot> playerList = PlayerUtility.GetAllPlayer();

        // プレイヤーリストの更新を購読し、UIを更新する
        foreach (var root in playerList)
        {
            if (root == null) { continue; }
            if (root.PlayerIndex.Value < 0) { continue; }


            // プレイヤーの名前とHPスライダーの初期設定
            string playerName = PlayerDataManager.Instance.GetPlayerNameByIndex(root.PlayerIndex.Value);

            Debug.Log($"[GameUIManager] PlayerIndex: {root.PlayerIndex.Value}, Name: {playerName}");


            SetPlayerName(root.PlayerIndex.Value, playerName);
            SetHealthSliderMaxValue(root.PlayerIndex.Value, 100);

            // 2. HP変化時の処理
            root.CurrentHealth.Subscribe(hp =>
            {
                UpdateHealth(root.PlayerIndex.Value, hp);
                
            }).AddTo(playerSubscriptions);


            // プレイヤーのアクティブ効果の変更を購読し、UIのステータス表示を更新する
            root.ActiveEffects.ObserveChanged()
                .Subscribe(_ =>
                {
                    UpdateActiveEffects(root.PlayerIndex.Value, root.ActiveEffects.ToList());
                }).AddTo(playerSubscriptions);
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
            hpText[playerIndex].text = health.ToString();
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

