using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelectManager : NetworkBehaviour
{

    private bool isLocalMode = false; // オフラインデバッグモード

    [Header("UI Lineup")]
    [SerializeField] private Transform skillButtonParent;   // 全スキルボタンを生成する親のTransform
    [SerializeField] private SkillButton skillButtonPrefab; // スキルボタンのプレハブ

    [Header("Preview Slots (UI上の1～4番目の枠)")]
    [SerializeField] private SkillPreviewSlot[] previewSlots = new SkillPreviewSlot[4];
    [SerializeField] private Sprite emptySlotSprite;        // スキルが空の時の背景画像

    [Header("Decision UI")]
    [SerializeField] private Button decisionButton;         // 決定ボタンの参照

    // サイズを4にする 
    private Skill[] mySkills = new Skill[4];

    // AssetLoaderからロードしたすべてのスキルリスト
    [SerializeField] private List<Skill> allSkills = new List<Skill>();

    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        // カーテンを開ける
        CurtainManager.Instance.OpenAsync(GetType().Name).Forget();

        // デバッグモードの判定
        isLocalMode = PlayerDataManager.Instance.IsLocalMode;

        InitializeSkillList();
        UpdatePreviewUI();

        if (decisionButton != null)
        {
            decisionButton.onClick.AddListener(OnDecisionButtonPressed);
        }

        // デバッグモード時はサーバー/ネットワークの監視設定をスキップする
        if (isLocalMode)
        {
            Debug.Log("[DebugMode] ネットワーク監視をスキップして起動します。");
            return;
        }

        // --- ネットワーク接続時のみ実行される処理 ---
        // ネットワークが未起動だと IsServer が例外を吐く場合があるため、安全対策
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsServer)
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
        if (isLocalMode || !IsServer) return;

        var playerDataList = PlayerDataManager.Instance.AllPlayerData;
        for (int i = 0; i < playerDataList.Count; i++)
        {
            if (PlayerDataManager.Instance.TryGetPlayerData(i, out var data))
            {
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

        // 4. UIの制御
        if (decisionButton != null) decisionButton.interactable = false;

        // --- デバッグモード時の分岐 ---
        if (isLocalMode)
        {
            Debug.Log("[DebugMode] 決定ボタン押下。オフラインでバトルシーンへ遷移します。");
            GoToBattleSceneOffline().Forget();
            return; // 以降のネットワーク通信をブロック
        }

        // 3. サーバーへ「スキル構成」と「準備完了(Ready=true)」を送信 (オンライン時のみ)
        SubmitSelectedSkillsAndReadyServerRpc(skillNumbers);
    }

    /// <summary>
    /// デバッグモード用：サーバー通信を待たずに単独でシーン遷移する
    /// </summary>
    private async UniTaskVoid GoToBattleSceneOffline()
    {
        await CurtainManager.Instance.CloseAsync("Ready!", GetType().Name); // カーテンを閉じる演出

        await UniTask.Delay(TimeSpan.FromSeconds(1.5f)); // 演出用ディレイ
        GameSceneManager.Instance.LoadLocalScene("Game"); // オフライン用のシーン遷移
    }

    /// <summary>
    /// スキルデータの上書きと、Ready完了を同時に行うServerRpc
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SubmitSelectedSkillsAndReadyServerRpc(int[] selectedSkillNos, ServerRpcParams rpcParams = default)
    {
        if (isLocalMode) return; // 念のためブロック

        ulong clientId = rpcParams.Receive.SenderClientId;

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.Server_UpdatePlayerSkills(clientId, selectedSkillNos);
            PlayerDataManager.Instance.Server_SetPlayerReady(clientId, true);
        }
    }

    /// <summary>
    /// 【サーバー専用】全員がスキル選択を終えたか確認し、
    /// 完了していれば本番のバトルシーンへ遷移する
    /// </summary>
    private async UniTaskVoid CheckAllPlayersReadyAndGoToBattle()
    {
        if (isLocalMode) return;

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
        allSkills = AssetLoader.Instance.LoadAllSkills;

        foreach (Transform child in skillButtonParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var skillData in allSkills)
        {
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
    /// スキル選択ボタンが押された時の処理
    /// </summary>
    private void OnSkillButtonClicked(Skill selectedSkill)
    {
        if (IsSkillAlreadySelected(selectedSkill))
        {
            Debug.LogWarning($"{selectedSkill.GetSkillName()} は既に選択されています。");
            return;
        }

        for (int i = 0; i < mySkills.Length; i++)
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
    private void RemoveSkillFromSlot(int slotIndex)
    {
        int arrayIndex = slotIndex;

        if (mySkills[arrayIndex] == null) return;

        Debug.Log($"選択枠 {arrayIndex} のスキルを外しました: {mySkills[arrayIndex].GetSkillName()}");
        mySkills[arrayIndex] = null;

        PackSkillsLeft();
        UpdatePreviewUI();
    }

    /// <summary>
    /// 選択枠（インデックス0〜3）のスキルを左詰めに整理する処理
    /// </summary>
    private void PackSkillsLeft()
    {
        List<Skill> tempLeftPackedList = new List<Skill>();

        for (int i = 0; i < mySkills.Length; i++)
        {
            if (mySkills[i] != null)
            {
                tempLeftPackedList.Add(mySkills[i]);
            }
        }

        Array.Clear(mySkills, 0, mySkills.Length);
        for (int i = 0; i < tempLeftPackedList.Count; i++)
        {
            mySkills[i] = tempLeftPackedList[i];
        }
    }

    /// <summary>
    /// mySkills配列[0~3]の状態を画面下の4つのプレビューUIに同期・反映させる
    /// </summary>
    private void UpdatePreviewUI()
    {
        for (int i = 0; i < previewSlots.Length; i++)
        {
            int slotIndex = i; // クロージャ対策
            SkillPreviewSlot slot = previewSlots[i];

            slot.removeButton.onClick.RemoveAllListeners();

            int arrayIndex = i;

            if (mySkills[arrayIndex] != null)
            {
                slot.skillImage.gameObject.SetActive(true);
                slot.skillImage.sprite = mySkills[arrayIndex].GetSprite();

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