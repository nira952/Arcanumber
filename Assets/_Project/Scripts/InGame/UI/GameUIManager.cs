using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R3;
using DG.Tweening;

public class GameUIManager : SingletonMonoBehaviour<GameUIManager>
{
    [Serializable]
    private class PlayerEffectBlock
    {
        // 事前にインスペクターで5枚程度の Image をセットしておく
        public List<Image> availableImages = new List<Image>();

        /// <summary>
        /// 全てのイメージ枠を一旦非表示にする
        /// </summary>
        public void HideAll()
        {
            foreach (var img in availableImages)
            {
                if (img != null)
                {
                    img.gameObject.SetActive(false);
                }
            }
        }
    }

    [Header("ステータス関係 (プレイヤーごと)")]
    [SerializeField] private PlayerEffectBlock[] playerEffectBlocks = new PlayerEffectBlock[4];

    [SerializeField] private Slider[] healthSliders = new Slider[4];
    [SerializeField] private TextMeshProUGUI[] hpText;
    [SerializeField] private TextMeshProUGUI[] playerNameTexts = new TextMeshProUGUI[4];
    [SerializeField] private TextMeshProUGUI timerText;

    [SerializeField] private TextMeshProUGUI gameStateText;

    protected override void Awake()
    {
        base.Awake();
        // 起動時に全非表示にしておく
        foreach (var block in playerEffectBlocks)
        {
            block.HideAll();
        }

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
        if (hpText != null && playerIndex < hpText.Length)
        {
            hpText[playerIndex].text = health.ToString();
        }
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
    public void UpdateTimerDisplay(int totalSeconds)
    {
        if (timerText == null) return;

        TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
        // mm:ss で「05:00」のような2桁固定表記になります
        timerText.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }


    /// <summary>
    /// R3 の ReactiveProperty<List<EffectAbility>> を購読して UI とバインドする
    /// </summary>
    public void BindPlayerStatus(int playerIndex, ReactiveProperty<List<EffectAbility>> activeEffects)
    {
        if (playerIndex < 0 || playerIndex >= playerEffectBlocks.Length) return;
        if (activeEffects == null) return;

        activeEffects
            .Subscribe(effects =>
            {
                UpdateActiveEffects(playerIndex, effects);
            })
            .AddTo(this);
    }

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
}

