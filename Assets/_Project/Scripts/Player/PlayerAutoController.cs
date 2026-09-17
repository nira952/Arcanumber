using nira.Demo;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using R3;
using Unity.VisualScripting;

public class PlayerAutoController : MonoBehaviour, IPlayerActionHandler
{
    private PlayerRoot root;
    private NetworkPlayer player;
    private AutoInputController autoContoller;

    private Rigidbody2D rigidbody2D;

    private const int ENEMY_INDEX = 1;

    private const string  ENEMY_NAME = "Enemy";

    private List<EffectAbility> activeEffects = new List<EffectAbility>();

    public bool CanProcessInput
    {
        get
        {
            // ゲーム状態がPlaying かつ ダウンしていない時のみ入力を許可
            if (GameManager.Instance != null && GameManager.Instance.CurrentState.Value != GameState.Playing) return false;
            return !root.IsDown.Value;
        }
    }


    private void Awake()
    {
        // もしオンラインモードならこのコンポーネントを破棄
        if (!PlayerDataManager.Instance.IsLocalMode)
        {
            Destroy(this); 
            return;
        }

        root = GetComponent<PlayerRoot>();
        player = GetComponent<NetworkPlayer>();
        rigidbody2D = GetComponent<Rigidbody2D>();

        // 生成時に初期化処理を行う
        Initialize();
    }

    private void Update()
    {
        // 所有者でない場合
        if (!CanProcessInput)
        {
            rigidbody2D.linearVelocity = Vector2.zero; // 移動を停止
        }
    }

    private void Initialize()
    {
        // Inputコンポーネントを無効化
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;

        // AutoInputControllerを追加して、敵としての自動操作を行う
        autoContoller = root.AddComponent<AutoInputController>();

        root.gameObject.tag = "Enemy";

        // 敵のプレイヤーインデックスを設定
        int assignedIndex = ENEMY_INDEX;
        root.PlayerIndex.Value = assignedIndex;

        // このスクリプト内で IsDown が変更されたら、勝敗判定を行う
        root.IsDown.Subscribe(v =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckFinishCondition();
            }
        }).AddTo(this);

        // rootのActiveEffectsをこのスクリプトに反映する
        root.ActiveEffects.Subscribe(localList =>
        {
            activeEffects.Clear();
            // クローンを作成してコピー
            activeEffects = localList;
            
        }).AddTo(this);


        // --- UIの初期化処理 ---

        if (GameUIManager.Instance == null)
        {
            Debug.LogWarning("GameUIManager is not found in the scene.");
            return;
        }

        // UIManagerのインスタンスを取得
        GameUIManager uIManager = GameUIManager.Instance;

        // UIにIndex変化時の処理を登録
        root.PlayerIndex.Where(idx => idx != -1).Take(1).Subscribe(idx => {

            string name = ENEMY_NAME;
            // UIにプレイヤー名を設定
            uIManager.SetPlayerName(idx, name);
            // UIに最大HPを設定
            uIManager.SetHealthSliderMaxValue(idx, 100);

        }).AddTo(this);

        // UIにHPの変化時の処理を登録
        root.CurrentHealth.Subscribe(hp => {
            if (root.PlayerIndex.Value != -1)
            {
                uIManager.UpdateHealth(root.PlayerIndex.Value, hp);
            }
        }).AddTo(this);

        // 全て終わったらrootの初期化処理を呼び出す
        root.Initialize(this, autoContoller);
    }

    public void RequestSkillUse()
    {
        player.UseCurrentSkill(); // スキルのアクションを呼び出す

    }


    public void RequestAttack()
    {
        player.UseAttack(); // 攻撃のアクションを呼び出す
    }
}
