using ObservableCollections;
using R3;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerRoot))]
[RequireComponent(typeof(PlayerInputBinder))]
public class PlayerNetworkController : NetworkBehaviour, IPlayerInputMediator
{
    private PlayerRoot root;
    private PlayerInputBinder inputBinder;

    private PlayerInputController inputController;

    private readonly NetworkVariable<int> netPlayerIndex = new(-1);
    private readonly NetworkVariable<FixedString64Bytes> netPlayerName =new("Player"); 
    private readonly NetworkVariable<float> netCurrentHealth = new(100);
    private readonly NetworkVariable<bool> netIsDown = new(false);
    private readonly NetworkList<NetworkEffectData> netActiveEffects = new NetworkList<NetworkEffectData>();


    private void Awake()
    {
        if (PlayerDataManager.Instance.IsLocalMode)
        {
            Destroy(this); // ローカルモードではこのコンポーネントを破棄
            return;
        }

    }

    public override void OnNetworkSpawn()
    {
        // --- コンポーネントの取得 ---
        inputController = GetComponent<PlayerInputController>(); // 入力コントローラーの取得
        root            = GetComponent<PlayerRoot>();            // メインスクリプトを取得
        inputBinder     = GetComponent<PlayerInputBinder>();     // 入力バインダーの取得

        PlayerUtility.RegisterPlayer(root);

        if (IsServer)
        {
            // サーバー側でプレイヤーのインデックスを設定
            int assignedIndex = PlayerDataManager.Instance.GetLobbyIndexByClientId(OwnerClientId);
            root.PlayerIndex.Value = assignedIndex;

            Skill[] skills = PlayerDataManager.Instance.GetPlayerSkillsByIndex(assignedIndex);
            Arcana arcana = PlayerDataManager.Instance.GetPlayerArcanaByIndex(assignedIndex);

            root.SetSkill(skills);
            root.SetArcana(arcana);
        }


        // 所有者でない場合、Inputコンポーネントを停止
        if (!IsOwner)
        {
            if (inputController != null) { inputController.enabled = false; }

            // 所有者でない場合、タグを "Enemy" に設定
            root.gameObject.tag = "Enemy";
        }

        // --- 入力バインダーの初期化 ---

        // バインダーに現在のコントローラーを設定し、入力イベントを購読する
        inputBinder.Initialize(this);

        // --- ネットワーク同期の初期化 ---

        if (IsServer)
        {


            // サーバー側で ReactiveProperty の変更を NetworkVariable に同期
            root.PlayerIndex.Subscribe(v => netPlayerIndex.Value = v).AddTo(this);
            root.PlayerName.Subscribe(v => netPlayerName.Value = v).AddTo(this);
            root.CurrentHealth.Subscribe(v => netCurrentHealth.Value = v).AddTo(this);
            root.IsDown.Subscribe(v => netIsDown.Value = v).AddTo(this);

            // --- NetworkList の同期処理 ---

            // 要素が「追加」された時だけ NetworkList に Add する
            root.ActiveEffects.ObserveAdd().Subscribe(e =>
            {
                // 1. e.Value (EffectAbility) 自体の null チェック
                if (e.Value == null)
                {
                    Debug.LogError("[PlayerNetworkController] 追加された EffectAbility (e.Value) が null です。");
                    return;
                }

                Effect effectSO = e.Value.GetEffect();

                // 2. e.Value.GetEffect() で取得した effectSO の null チェック
                if (effectSO == null)
                {
                    Debug.LogError($"[PlayerNetworkController] {e.Value} の GetEffect() が null を返しました。");
                    return;
                }

                // 両方 null でないことが確認できてからデータを作成
                netActiveEffects.Add(new NetworkEffectData
                {
                    EffectType = effectSO.GetEffectList(),
                    IsUp = effectSO.GetIsUp(),
                    IsDisplay = e.Value.IsDisplay(),
                    Time = e.Value.GetTime(),
                    Value = e.Value.GetValue()
                });
            }).AddTo(this);

            // 要素が「削除」された時だけ NetworkList から RemoveAt する
            root.ActiveEffects.ObserveRemove().Subscribe(e =>
            {
                netActiveEffects.RemoveAt(e.Index);
            }).AddTo(this);

            // 要素が「全削除」された時だけ NetworkList を Clear する
            root.ActiveEffects.ObserveClear().Subscribe(_ =>
            {
                netActiveEffects.Clear();
            }).AddTo(this);

        }
        else
        {
            // クライアント側で NetworkVariable の変更を ReactiveProperty に同期
            netPlayerIndex.AsObservable().Subscribe(v => root.PlayerIndex.Value = v).AddTo(this);
            netPlayerName.AsObservable().Subscribe(v => root.PlayerName.Value = v.ToString()).AddTo(this);
            netCurrentHealth.AsObservable().Subscribe(v => root.CurrentHealth.Value = v).AddTo(this);
            netIsDown.AsObservable().Subscribe(v => root.IsDown.Value = v).AddTo(this);

            // イベントリスナーの登録
            netActiveEffects.OnListChanged += HandleNetworkListChanged;

            // 途中参加などで既にネットワークリストに要素がある場合の初期同期
            InitializeEffectsFromNetworkList();
        }

        // プレイヤーの初期化を実行
        root.OwnerInitialize();
    }


    // --- 各アクション処理 ---

    /// <summary>
    /// ローカルで攻撃入力を処理し、攻撃スキルを発動してサーバーへ攻撃要求を送信する。
    /// </summary>
    public void OnAttackTriggered()
    {
        // 攻撃を実行する
        root.ExecuteAttack();

        // サーバーへ攻撃要求を送信
        RequestAttackServerRpc();
    }

    [ServerRpc] private void RequestAttackServerRpc() => ExecuteAttackClientRpc();
    [ClientRpc] private void ExecuteAttackClientRpc() 
    {
        // 自分のクライアントでは既に攻撃処理を行っているので、所有者でない場合のみ実行
        if (!IsOwner)
        {
            // 攻撃を実行する
            root.ExecuteAttack();
        }

    }

    // ローカルで移動入力を処理する
    public void OnMoveTriggered(float direction)
    {
        root.ExecuteMove(direction); // 移動のアクションを呼び出す

        // 位置の同期は NetworkTransform に任せる
    }

    public void OnJumpTriggered()
    {
        root.ExecuteJump();

        // ジャンプの同期は NetworkTransform に任せる
    }

    public void OnSkillSelectTriggered(int skillIndex)
    {
        root.ExecuteSkillSelect(skillIndex);
    }

    public void OnSkillUseTriggered()
    {
        if (!IsOwner) return;

        root.ExecuteSkillUse();
    }



    // --- NetworkList の変更を処理するメソッド ---
    private void HandleNetworkListChanged(NetworkListEvent<NetworkEffectData> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<NetworkEffectData>.EventType.Add:
            case NetworkListEvent<NetworkEffectData>.EventType.Insert:
                var newAbility = CreateEffectAbility(changeEvent.Value);
                if (newAbility != null)
                {
                    // インデックス指定または末尾追加
                    if (changeEvent.Index < root.ActiveEffects.Count)
                    {
                        root.ActiveEffects.Insert(changeEvent.Index, newAbility);
                    }
                    else
                    {
                        root.ActiveEffects.Add(newAbility);
                    }
                }
                break;

            case NetworkListEvent<NetworkEffectData>.EventType.Remove:
            case NetworkListEvent<NetworkEffectData>.EventType.RemoveAt:
                if (changeEvent.Index < root.ActiveEffects.Count)
                {
                    root.ActiveEffects.RemoveAt(changeEvent.Index);
                }
                break;

            case NetworkListEvent<NetworkEffectData>.EventType.Value: // 値の更新
                var updatedAbility = CreateEffectAbility(changeEvent.Value);
                if (updatedAbility != null && changeEvent.Index < root.ActiveEffects.Count)
                {
                    root.ActiveEffects[changeEvent.Index] = updatedAbility;
                }
                break;

            case NetworkListEvent<NetworkEffectData>.EventType.Clear:
                root.ActiveEffects.Clear();
                break;
        }
    }

    // 初期化（途中参加クライアント用）
    private void InitializeEffectsFromNetworkList()
    {
        root.ActiveEffects.Clear();
        foreach (var netData in netActiveEffects)
        {
            var ability = CreateEffectAbility(netData);
            if (ability != null)
            {
                root.ActiveEffects.Add(ability);
            }
        }
    }

    // EffectData から EffectAbility を生成する共通ヘルパーメソッド
    private EffectAbility CreateEffectAbility(NetworkEffectData netData)
    {
        Effect effectSO = EffectRegistry.Get(netData.EffectType, netData.IsUp);
        if (effectSO == null) return null;

        return new EffectAbility(
            effectSO,
            netData.IsDisplay,
            netData.Time,
            netData.Value
        );
    }

    // バインドの解除
    public override void OnNetworkDespawn()
    {
        if (IsClient && !IsServer)
        {
            netActiveEffects.OnListChanged -= HandleNetworkListChanged;
        }
    }

}