using Cysharp.Threading.Tasks;
using R3; 
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelectManager : NetworkBehaviour
{
    [Header("UI Lineup")]
    [SerializeField] private Transform skillButtonParent;   // 全スキルボタンを生成する親のTransform
    [SerializeField] private SkillButton skillButtonPrefab; // スキルボタンのプレハブ

    [Header("Preview Slots (UI上の1～4番目の枠)")]
    [SerializeField] private SkillPreviewSlot[] previewSlots = new SkillPreviewSlot[4];
    [SerializeField] private Sprite emptySlotSprite;        // スキルが空の時の背景画像

    [Header("Fixed Skill Settings")]
    [SerializeField] private Skill defaultFixedSkill;       // 配列0番目に強制固定するスキル

    [Header("Decision UI")]
    [SerializeField] private Button decisionButton;         // 決定ボタンの参照
    [SerializeField] private GameObject waitingOverlay;

    // サイズを5にする (0: 固定枠, 1～4: 選択枠)
    private Skill[] mySkills = new Skill[5];

    // AssetLoaderからロードしたすべてのスキルリスト
    private List<Skill> allSkills = new List<Skill>();


    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        // 0番目の枠に固定スキルを代入
        if (defaultFixedSkill != null) mySkills[0] = defaultFixedSkill;

        InitializeSkillList();
        UpdatePreviewUI();

        if (decisionButton != null)
        {
            decisionButton.onClick.AddListener(OnDecisionButtonPressed);
        }

        // --- ここから追加：サーバー側での準備状態リセットと監視 ---
        if (IsServer)
        {
            // 1. 新しいシーンに入ったので、一旦全員の準備状態を false に戻す
            ResetAllPlayersReadyStatus();

            // 2. 【R3】全員がスキル選択を終えて準備完了になったかを NetworkList から監視
            Observable.FromEvent<NetworkList<PlayerNetworkData>.OnListChangedDelegate, NetworkListEvent<PlayerNetworkData>>(
                h => (ev) => h(ev),
                h => PlayerDataManager.Instance.AllPlayerData.OnListChanged += h,
                h => PlayerDataManager.Instance.AllPlayerData.OnListChanged -= h
            )
            .Subscribe(_ =>
            {
                CheckAllPlayersReadyAndGoToBattle().Forget();
            })
            .AddTo(_disposables);
        }
    }

    private void OnDestroy()
    {
        _disposables.Dispose(); // メモリリーク防止
    }

    /// <summary>
    /// 【サーバー専用】シーン開始時に全員のReady状態を一度リセットする
    /// </summary>
    private void ResetAllPlayersReadyStatus()
    {
        if (!IsServer) return;

        var playerDataList = PlayerDataManager.Instance.AllPlayerData;
        for (int i = 0; i < playerDataList.Count; i++)
        {
            if (PlayerDataManager.Instance.TryGetPlayerData(i, out var data))
            {
                // 先ほど PlayerDataManager に追加したメソッドを利用して一斉に解除
                PlayerDataManager.Instance.Server_SetPlayerReady(data.ClientId, false);
            }
        }
    }

    /// <summary>
    /// 決定ボタンが押されたときのローカル処理
    /// </summary>
    private void OnDecisionButtonPressed()
    {
        // 枠がすべて埋まっているかバリデーション
        for (int i = 0; i < mySkills.Length; i++)
        {
            if (mySkills[i] == null)
            {
                Debug.LogWarning($"枠 {i} が空欄のため、まだ決定できません。");
                return;
            }
        }

        // 1. ローカルの PlayerDataManager に自分の選択を保存
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetLocalSkills(mySkills);
        }

        // 2. スキル番号（int配列）に変換
        int[] skillNumbers = new int[mySkills.Length];
        for (int i = 0; i < mySkills.Length; i++)
        {
            skillNumbers[i] = mySkills[i].GetSkillNo();
        }

        // 3. サーバーへ「スキル構成」と「準備完了(Ready=true)」を送信
        SubmitSelectedSkillsAndReadyServerRpc(skillNumbers);

        // 4. UIの制御
        if (decisionButton != null) decisionButton.interactable = false;
        if (waitingOverlay != null) waitingOverlay.SetActive(true); // 待機中画面を出す
    }

    /// <summary>
    /// スキルデータの上書きと、Ready完了を同時に行うServerRpc
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SubmitSelectedSkillsAndReadyServerRpc(int[] selectedSkillNos, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (PlayerDataManager.Instance != null)
        {
            // スキルを同期用NetworkListに格納
            PlayerDataManager.Instance.Server_UpdatePlayerSkills(clientId, selectedSkillNos);

            // 同時に、このプレイヤーの準備状態を true にする
            PlayerDataManager.Instance.Server_SetPlayerReady(clientId, true);
        }
    }

    /// <summary>
    /// 【サーバー専用】全員がスキル選択を終えたか確認し、
    /// 完了していれば本番のバトルシーンへ遷移する
    /// </summary>
    private async UniTaskVoid CheckAllPlayersReadyAndGoToBattle()
    {
        var playerDataList = PlayerDataManager.Instance.AllPlayerData;
        if (playerDataList.Count == 0) return;

        // 全員の IsReady == true かをチェック
        foreach (var player in playerDataList)
        {
            if (!player.IsReady) return; // 一人でも未完了なら弾く
        }

        Debug.Log("[Server] 全プレイヤーのスキル選択が完了しました！バトルシーンへ移行します。");

        await UniTask.Delay(TimeSpan.FromSeconds(1.5f)); // 演出用ディレイ

        GameSceneManager.Instance.LoadNetworkScene("Game");

    }

    /// <summary>
    /// AssetLoaderからスキル一覧を取得し、選択用のボタンを生成する
    /// </summary>
    private void InitializeSkillList()
    {
        allSkills = AssetLoader.LoadAllSkills();

        foreach (Transform child in skillButtonParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var skillData in allSkills)
        {
            // 【工夫】もし固定スキルと同じスキルなら、選択ボタン一覧には生成しない（重複防止）
            if (defaultFixedSkill != null && skillData.GetSkillNo() == defaultFixedSkill.GetSkillNo())
            {
                continue;
            }

            SkillButton btnInstance = Instantiate(skillButtonPrefab, skillButtonParent);
            btnInstance.skill = skillData;
            btnInstance.skillName = skillData.GetSkillName();
            if (btnInstance.skillImage != null)
            {
                btnInstance.skillImage.sprite = skillData.GetSprite();
            }

            btnInstance.skillButton.onClick.AddListener(() => OnSkillButtonClicked(skillData));
        }
    }

    /// <summary>
    /// スキル選択ボタンが押された時の処理（インデックス1〜4の空きに左詰めで追加）
    /// </summary>
    private void OnSkillButtonClicked(Skill selectedSkill)
    {
        if (IsSkillAlreadySelected(selectedSkill))
        {
            Debug.LogWarning($"{selectedSkill.GetSkillName()} は既に選択されています。");
            return;
        }

        // 変更点：ループを「1」から開始し、インデックス1~4の範囲を探す
        for (int i = 1; i < mySkills.Length; i++)
        {
            if (mySkills[i] == null)
            {
                mySkills[i] = selectedSkill;
                Debug.Log($"選択枠 {i} (配列インデックス {i}) にセットしました: {selectedSkill.GetSkillName()}");

                UpdatePreviewUI();
                return;
            }
        }

        Debug.LogWarning("選択スキル枠（4つ）が満杯です。どれかを外してください。");
    }

    /// <summary>
    /// プレビューのボタンが押された時に、そのスロットのスキルを外す
    /// </summary>
    /// <param name="slotIndex">UI上のインデックス (0～3。配列上は 1～4 に対応)</param>
    private void RemoveSkillFromSlot(int slotIndex)
    {
        // UIの要素番号(0~3)に+1して、配列のインデックス(1~4)に変換
        int arrayIndex = slotIndex + 1;

        if (mySkills[arrayIndex] == null) return;

        Debug.Log($"選択枠 {arrayIndex} のスキルを外しました: {mySkills[arrayIndex].GetSkillName()}");
        mySkills[arrayIndex] = null;

        // 選択枠（1〜4）の中だけで左詰めにソート
        PackSkillsLeft();

        // UIを更新
        UpdatePreviewUI();
    }

    /// <summary>
    /// 選択枠（インデックス1〜4）のスキルを左詰めに整理する処理
    /// </summary>
    private void PackSkillsLeft()
    {
        List<Skill> tempLeftPackedList = new List<Skill>();

        // インデックス1〜4に入っているスキルだけを抽出
        for (int i = 1; i < mySkills.Length; i++)
        {
            if (mySkills[i] != null)
            {
                tempLeftPackedList.Add(mySkills[i]);
            }
        }

        // インデックス1以降を一旦クリアして、左詰めで再代入
        System.Array.Clear(mySkills, 1, mySkills.Length - 1);
        for (int i = 0; i < tempLeftPackedList.Count; i++)
        {
            mySkills[i + 1] = tempLeftPackedList[i]; // 配列の1番目から詰めていく
        }
    }

    /// <summary>
    /// mySkills配列[1~4]の状態を画面下の4つのプレビューUIに同期・反映させる
    /// </summary>
    private void UpdatePreviewUI()
    {
        // previewSlotsの要素数は4（UI上の枠1~4）
        for (int i = 0; i < previewSlots.Length; i++)
        {
            int slotIndex = i; // クロージャ対策
            SkillPreviewSlot slot = previewSlots[i];

            slot.removeButton.onClick.RemoveAllListeners();

            // 配列のインデックスは i + 1 (1〜4) を見に行く
            int arrayIndex = i + 1;

            if (mySkills[arrayIndex] != null)
            {
                slot.skillImage.gameObject.SetActive(true);
                slot.skillImage.sprite = mySkills[arrayIndex].GetSprite();

                // プレビューボタンが押されたら該当スロット(0~3)を外すイベントを登録
                slot.removeButton.onClick.AddListener(() => RemoveSkillFromSlot(slotIndex));
                slot.removeButton.interactable = true;
            }
            else
            {
                if (emptySlotSprite != null)
                {
                    slot.skillImage.sprite = emptySlotSprite;
                    slot.skillImage.gameObject.SetActive(true);
                }
                else
                {
                    slot.skillImage.gameObject.SetActive(false);
                }

                slot.removeButton.interactable = false;
            }
        }
    }

    /// <summary>
    /// スキル重複チェック用のヘルパー
    /// </summary>
    private bool IsSkillAlreadySelected(Skill skill)
    {
        // 変更点：全インデックス（0〜4）を通して重複がないかチェック
        foreach (var s in mySkills)
        {
            if (s != null && s.GetSkillNo() == skill.GetSkillNo())
            {
                return true;
            }
        }
        return false;
    }

    public Skill[] GetSelectedSkills()
    {
        return mySkills;
    }
}