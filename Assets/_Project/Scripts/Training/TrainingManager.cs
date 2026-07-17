using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// トレーニングモードの全体を管理するクラス
/// </summary>
public class TrainingManager : SingletonMonoBehaviour<TrainingManager>
{
    [SerializeField] private NetworkPlayer[] all;
    private List<Arcana> allArcana = new List<Arcana>();
    private List<Skill> allSkill = new List<Skill>();

    int pNum = 0;    //プレイヤーの番号


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadManager.Instance.ArcanaLoadExcel();

        Initialise();
        TrainingUIManager.Instance.Initialize();
    }

    /// <summary>
    /// 初期設定
    /// </summary>
    void Initialise()
    {
        //プレイヤーを辞書登録する
        foreach (NetworkPlayer p in all)
            PlayerUtility.RegisterPlayer(p);

        //自分が操作しているプレイヤーだけの処理
        foreach (NetworkPlayer p in all)
            p.Initialize(p.GetNetworkId() == pNum);

        BattleUIManager.Instance.Initialize(all, pNum);
        //ダメージエフェクト（アクション）の登録
        all[pNum].GetArcana().ExecuteArcanaEffect(ASkillCategory.DamageEffect, all[pNum]);

        //アルカナやスキルをロードする
        LoadAsset();
    }

    private void FixedUpdate()
    {
        //地面判定と移動
        bool isGrounded = all[pNum].GetPlayerController().PlayerPosUpdate();
        //ジャンプリセット
        if (isGrounded)
            all[pNum].JumpReset();
    }

    void Update()
    {
        //プレイヤーの状態を更新
        foreach(NetworkPlayer p in all)
            PlayerUtility.UpdatePlayerSystem(p);

        //クールタイムの更新
        if (all[pNum] != null)
            all[pNum].UpdateAllCoolTimes(Time.deltaTime);
        BattleUIManager.Instance.UpdateSkillCoolTimeUI(all[pNum]);
    }

    /// <summary>
    /// アルカナを並べ替える（練習場用）
    /// </summary>
    void LoadAsset()
    {
        //アルカナを並べ替える
        allArcana = LoadManager.Instance.GetArcanas;

        //スキルを入れ替える
        allSkill = AssetLoader.Instance.LoadAllSkills;
    }

    /// <summary>
    /// リセットするメソッド
    /// </summary>
    public void ResetPlayerStatus(int playerNum)
    {
        NetworkPlayer p = GetPlayer(playerNum);

        p.ResetToInitialState();    //ステータスをリセットする
        p.SetSkillNo(0);    //最初のスキルに戻す

        //UIとエイムをその状態に合わせて再構築
        BattleUIManager.Instance.Initialize(all, 0);

        //エイムリセット
        p.GetPlayerController().GetAimCursor()
            .SelectAim(p.GetNoSkill().GetAimSelect());

        //UIのフレームも 0 番に合わせる
        BattleUIManager.Instance.SkillFrameChange(p);
    }

    //ゲッター
    public NetworkPlayer GetPlayer(int num) => all[num];
    public NetworkPlayer[] GetNetworkPlayers() => all;
    public List<Arcana> GetArcanas() => allArcana;
    public List<Skill> GetSkills() => allSkill;
}
