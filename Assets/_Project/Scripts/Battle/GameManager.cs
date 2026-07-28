//using UnityEngine;

///// <summary>
///// ゲームの流れを管理するクラス
///// </summary>
//public class GameManager : SingletonMonoBehaviour<GameManager>
//{
//    [Header("プレイヤーの管理")]
//    //のちに配列にする
//    public NetworkPlayer player;

//    //[SerializeField] private EffectRegistry effectRegistry;

//    public GameState gameState; //現在のゲームの状態

//    public enum GameState { Waiting, Playing, Ending }

//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//        Initialize();
//    }

//    void Initialize()
//    {
//        player.Initialize(true);
//        //BattleUIManager.Instance.Initialize(player, player.GetNetworkId());
//        //ダメージエフェクト（アクション）の登録
//        player.GetArcana().ExecuteArcanaEffect(ASkillCategory.DamageEffect, player);
//    }

//    private void FixedUpdate()
//    {
//        //地面判定と移動
//        bool isGrounded = player.GetPlayerController().PlayerPosUpdate();
//        //ジャンプリセット
//        if (isGrounded)
//            player.JumpReset();
//    }

//    void Update()
//    {
//        //プレイヤーの状態を更新
//        PlayerUtility.UpdatePlayerSystem(player);
//        //クールタイムの更新
//        if (player != null)
//            player.UpdateAllCoolTimes(Time.deltaTime);
//        BattleUIManager.Instance.UpdateSkillCoolTimeUI(player);
//    }
//}
