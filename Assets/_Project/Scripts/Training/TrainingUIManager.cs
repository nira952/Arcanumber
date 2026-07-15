using NaughtyAttributes;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// トレーニング用のUIを管理するクラス
/// </summary>
public class TrainingUIManager : SingletonMonoBehaviour<TrainingUIManager>
{
    //アルカナ用のUI
    [Label("アルカナ選択UI")][SerializeField] private GameObject trainingArcanaUI;
    [Label("アルカナのボタン用のTransform")][SerializeField] private Transform trainingArcanaContent;
    [Label("アルカナボタンPrefab")][SerializeField] private GameObject trainingArcanaButton;
    [Label("アルカナ概要テキスト")][SerializeField] private TextMeshProUGUI trainingAText;
    [Label("持っているアルカナのテキスト")][SerializeField] private TextMeshProUGUI haveArcanaText;

    //スキル用のUI
    [Label("スキル選択UI")][SerializeField] private GameObject trainingSkillUI;
    [Label("スキル欄用のTransform")][SerializeField] private Transform traningSkillContent;
    [Label("スキルボタンPrefab")][SerializeField] private GameObject trainingSkillButton;
    [Label("スキル名テキスト")][SerializeField] private TextMeshProUGUI trainingSkillNameText;
    [Label("スキル概要テキスト")][SerializeField] private TextMeshProUGUI trainingSkillExText;
    [Label("持っているスキル一覧イメージ")][SerializeField] private Image[] myHaveSImg;
    [Label("持っているスキル枠イメージ")][SerializeField] private GameObject[] myHaveFrameImg;

    private int slotNum = 0;    //変更するスキルの番号

    /// <summary>
    /// 初期設定
    /// </summary>
    public void Initialize()
    {
        ArcanaSetUI();
        SkillSetUI();
        ChangeArcanaUI(false);
    }

    /// <summary>
    /// 変える用のアルカナセットUI
    /// </summary>
    void ArcanaSetUI()
    {
        //アルカナスキルを全部取得
        List<Arcana> list = TrainingManager.Instance.GetArcanas();
        foreach (Arcana arcana in list)
        {
            //ボタンを生成
            GameObject aButton = Instantiate(trainingArcanaButton, trainingArcanaContent);
            TextMeshProUGUI btnText = aButton.GetComponentInChildren<TextMeshProUGUI>();

            //ボタンアクションの追加
            Button btnComponent = aButton.GetComponent<Button>();
            if (btnComponent != null)
            {
                //アルカナを引数にするクリックイベント
                btnComponent.onClick.AddListener(() => ChangeArcana(arcana));
                //ホバーイベント
                AddHoverEvent(aButton,
                    () => HoverArcanaSkill(arcana.GetArcanaEX()),
                    () => trainingAText.text = "");
            }
            //ボタンの名前をアルカナにする
            if (btnText != null)
                btnText.text = $"{arcana.GetArcanaName} （{(arcana.GetIsFront() ? "正" : "逆")}位置）";
        }
        //持っているアルカナを変更
        GetHaveArcanaText();
    }

    /// <summary>
    /// スキル変更用のUI
    /// </summary>
    void SkillSetUI()
    {
        //すべてのスキルを収集
        List<Skill> list = TrainingManager.Instance.GetSkills();
        foreach (Skill skill in list)
        {
            GameObject sButton = Instantiate(trainingSkillButton, traningSkillContent);
            //ボタン画像を変える
            Image[] images = sButton.GetComponentsInChildren<Image>();
            if(images.Length > 1) images[1].sprite = skill.GetSprite();

            //ボタンアクションを入れる
            Button btnComponent = sButton.GetComponent<Button>();
            if (btnComponent != null)
            {
                //スキルを引数にする
                btnComponent.onClick.AddListener(() => ChangeSkill(skill));
                //ホバーイベント
                AddHoverEvent(sButton,
                    () => HoverSkill(skill.GetSkillName(), skill.GetSkillEx()),
                    () => { trainingSkillNameText.text = ""; trainingSkillExText.text = ""; });
            }
        }
        //スキルの画像を変える
        MyHaveSkillImage();
        SelectSkillButoon(slotNum);
    }

    /// <summary>
    /// スキル画像を変えるためのメソッド
    /// </summary>
    void MyHaveSkillImage()
    {
        Skill[] skills = PlayerUtility.FindPlayerByNo(0).GetSkill();

        for (int i = 0; i < myHaveSImg.Length; i++)
        {
            //もしスキルの数より画像の方が多い場合、エラーを防ぐためのチェック
            if (i < skills.Length)
                myHaveSImg[i].sprite = skills[i].GetSprite();
            else
                //スキルがない場合は画像を非表示にするなどの処理
                myHaveSImg[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ボタンのホバー設定
    /// </summary>
    void AddHoverEvent(GameObject button, UnityAction action, UnityAction onExit = null)
    {
        var hEvent = button.AddComponent<EventTrigger>();
        //かざしたときの処理
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => action());
        hEvent.triggers.Add(entry);
        //話したときの処理
        if (onExit != null)
        {
            var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener(_ => onExit());
            hEvent.triggers.Add(entryExit);
        }
    }

    /// <summary>
    /// ボタンをかざしたときの処理（アルカナ）
    /// </summary>
    void HoverArcanaSkill(string exText)
    {
        trainingAText.text = exText;
    }

    /// <summary>
    /// ボタンをかざしたときの処理（スキル）
    /// </summary>
    void HoverSkill(string sName, string exText)
    {
        trainingSkillNameText.text = sName;
        trainingSkillExText.text = exText;
    }

    /// <summary>
    /// アルカナボタンが押された時の処理
    /// </summary>
    void ChangeArcana(Arcana arcana)
    {
        //設定したアルカナを入れる
        PlayerUtility.FindPlayerByNo(0).SetArcana(arcana);
        //持っているアルカナを変更
        GetHaveArcanaText();
        //ここもUI変更の処理を書く
    }

    /// <summary>
    /// スキルボタンを選択した処理
    /// </summary>
    public void SelectSkillButoon(int skillNum)
    {
        slotNum = skillNum;
        foreach(GameObject frame in myHaveFrameImg)
            frame.SetActive(false);
        myHaveFrameImg[skillNum].SetActive(true);
    }

    /// <summary>
    /// スキルボタンが押された時の処理
    /// </summary>
    void ChangeSkill(Skill skill)
    {
        PlayerUtility.FindPlayerByNo(0).SetSkill(skill, slotNum);
        //ここでUI変更の処理を書く
        myHaveSImg[slotNum].sprite = skill.GetSprite();
    }

    /// <summary>
    /// アルカナUIの表示・非表示を切り替えるためのメソッド
    /// </summary>
    public void ChangeArcanaUI(bool isOn)
    {
        trainingArcanaUI.SetActive(isOn);
    }

    /// <summary>
    /// スキルUIの表示・非表示を切り替えるためのメソッド
    /// </summary>
    public void ChangeSkillUI(bool isOn)
    {
        trainingSkillUI.SetActive(isOn);
    }

    /// <summary>
    /// プレイヤーにダメージを与えるためのメソッド
    /// </summary>
    public void PlayerHPDamage()
    {
        PlayerUtility.FindPlayerByNo(0).TakeDamage(10);
    }

    /// <summary>
    /// 敵のHPを回復するためのメソッド
    /// </summary>
    public void EnemyHeal(int i)
    {
        PlayerUtility.FinalHeal(PlayerUtility.FindPlayerByNo(i), 10);
    }

    /// <summary>
    /// 全員のHPを回復するためのメソッド
    /// </summary>
    public void ResetGame()
    {
        foreach (NetworkPlayer p in TrainingManager.Instance.GetNetworkPlayers())
        {
            p.Heal(10000);
        }
        //リセット
        TrainingManager.Instance.ResetPlayerStatus(0);
    }

    /// <summary>
    /// AIの動きの静・動を切り替えるメソッド
    /// </summary>
    public void AIPlayerMove()
    {
        AIPlayer aiPlayer = PlayerUtility.FindPlayerByNo(1).GetComponent<AIPlayer>();
        aiPlayer.SetIsAIPlayer(!aiPlayer.GetIsAIPlayer());
    }

    /// <summary>
    /// 持っているアルカナスキルをテキストに入れる
    /// </summary>
    void GetHaveArcanaText()
    {
        //現在持っているアルカナの名前を入れる
        haveArcanaText.text
            = $"{PlayerUtility.FindPlayerByNo(0).GetArcana().GetArcanaName}" +
            $"（{(PlayerUtility.FindPlayerByNo(0).GetArcana().GetIsFront() ? "正" : "逆")}位置）";
    }
}
