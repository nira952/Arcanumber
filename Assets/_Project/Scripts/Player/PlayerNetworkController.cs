using nira.Demo;
using R3;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerRoot))]
public class PlayerNetworkController : NetworkBehaviour, IPlayerActionHandler
{
    private PlayerRoot root;
    private NetworkPlayer player;
    private PlayerInputController inputController;

    private Rigidbody2D rigidbody2D;

    private readonly NetworkVariable<int> netPlayerIndex = new(-1);
    private readonly NetworkVariable<float> netCurrentHealth = new(100);
    private readonly NetworkVariable<bool> netIsDown = new(false);
    private readonly NetworkList<NetworkEffectData> netActiveEffects = new NetworkList<NetworkEffectData>();

    // 自分のプレイヤーを操作できるかどうかを判定するプロパティ
    public bool CanProcessInput
    {
        get
        {
            // ネットワーク未生成、または所有者でない場合は入力を受け付けない
            if (!IsSpawned || !IsOwner) return false;
            // ゲーム状態がPlaying かつ ダウンしていない時のみ入力を許可
            if (GameManager.Instance != null && GameManager.Instance.CurrentState.Value != GameState.Playing) return false;
            return !root.IsDown.Value;
        }
    }

    private void Awake()
    {
        if (PlayerDataManager.Instance.IsLocalMode)
        {
            Destroy(this); // ローカルモードではこのコンポーネントを破棄
            return;
        }

        root = GetComponent<PlayerRoot>();
        player = GetComponent<NetworkPlayer>();
        rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 所有者でない場合
        if (!CanProcessInput)
        {
            rigidbody2D.linearVelocity = Vector2.zero; // 移動を停止
        }
    }

    public override void OnNetworkSpawn()
    {
        inputController = GetComponent<PlayerInputController>();

        // 所有者でない場合、Inputコンポーネントを停止
        if (!IsOwner)
        {
            if (inputController != null) inputController.enabled = false;
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;
            root.gameObject.tag = "Enemy";
        }

        Debug.Log($"[PlayerNetworkController] OnNetworkSpawn - IsOwner: {IsOwner}");

        if (IsServer)
        {
            int assignedIndex = PlayerDataManager.Instance.GetLobbyIndexByClientId(OwnerClientId);
            root.PlayerIndex.Value = assignedIndex;

            // サーバー側で ReactiveProperty の変更を NetworkVariable に同期
            root.PlayerIndex.Subscribe(v => netPlayerIndex.Value = v).AddTo(this);
            root.CurrentHealth.Subscribe(v => netCurrentHealth.Value = v).AddTo(this);

            // サーバー側で IsDown が変更されたら NetworkVariable に同期し、勝敗判定を行う
            root.IsDown.Subscribe(v =>
            {
                netIsDown.Value = v;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckFinishCondition();
                }
            }).AddTo(this);

            root.ActiveEffects.Subscribe(localList =>
            {
                netActiveEffects.Clear();
                foreach (var ability in localList)
                {
                    Effect effectSO = ability.GetEffect();
                    netActiveEffects.Add(new NetworkEffectData
                    {
                        EffectType = effectSO.GetEffectList(),
                        IsUp = effectSO.GetIsUp(),
                        IsDisplay = ability.IsDisplay(),
                        Time = ability.GetTime(),
                        Value = ability.GetValue()
                    });
                }
            }).AddTo(this);
        }
        else
        {
            Observable.FromEvent<NetworkVariable<int>.OnValueChangedDelegate, int>(
                h => (oldV, newV) => h(newV),
                h => netPlayerIndex.OnValueChanged += h,
                h => netPlayerIndex.OnValueChanged -= h
            ).Prepend(netPlayerIndex.Value).Subscribe(v => root.PlayerIndex.Value = v).AddTo(this);

            // クライアント側は root.CurrentHealth の同期のみを行う
            Observable.FromEvent<NetworkVariable<float>.OnValueChangedDelegate, float>(
                h => (oldV, newV) => h(newV),
                h => netCurrentHealth.OnValueChanged += h,
                h => netCurrentHealth.OnValueChanged -= h
            ).Prepend(netCurrentHealth.Value).Subscribe(v => root.CurrentHealth.Value = v).AddTo(this);

            // クライアント側は root.IsDown の同期のみを行う
            Observable.FromEvent<NetworkVariable<bool>.OnValueChangedDelegate, bool>(
                h => (oldV, newV) => h(newV),
                h => netIsDown.OnValueChanged += h,
                h => netIsDown.OnValueChanged -= h
            ).Prepend(netIsDown.Value).Subscribe(v =>
            {
                root.IsDown.Value = v;
            }).AddTo(this);

            netActiveEffects.OnListChanged += HandleNetworkListChanged;
            RebuildEffectsFromNetworkList();
        }

        // UIバインド
        root.PlayerIndex.Where(idx => idx != -1).Take(1).Subscribe(idx => InitializeUI(idx)).AddTo(this);

        root.CurrentHealth.Subscribe(hp => {
            if (root.PlayerIndex.Value != -1 && GameUIManager.Instance != null)
            {
                GameUIManager.Instance.UpdateHealth(root.PlayerIndex.Value, hp);
            }
        }).AddTo(this);

        
        root.Initialize(this, inputController);
    }


    private void InitializeUI(int index)
    {
        if (GameUIManager.Instance == null) return;
        
        string playerName = PlayerDataManager.Instance.GetPlayerNameByIndex(index);

        GameUIManager.Instance.SetPlayerName(index, playerName);
        GameUIManager.Instance.SetHealthSliderMaxValue(index, 100);
    }

    // --- 各アクション処理 (前回の実装まま) ---

    public void RequestAttack()
    {
        inputController.ExecuteAttackLocal();
        player.UseAttack(); // 攻撃のアクションを呼び出す
        RequestAttackServerRpc();
    }
    [ServerRpc] private void RequestAttackServerRpc() => ExecuteAttackClientRpc();
    [ClientRpc] private void ExecuteAttackClientRpc() 
    {
        // 自分のクライアントでは既に攻撃処理を行っているので、所有者でない場合のみ実行
        if (!IsOwner)
        {
            inputController.ExecuteAttackLocal();
            player.UseAttack(); // 攻撃のアクションを呼び出す
        }

    }

    public void RequestSkillUse()
    {
        player.UseCurrentSkill(); // スキルのアクションを呼び出す

        //// 2. サーバーへスキルの発動を要求
        RequestSkillUseServerRpc(root.SelectedSkillIndex.Value);
    }

    [ServerRpc]
    private void RequestSkillUseServerRpc(int skillIndex)
    {
        // 他クライアントへ演出の再生を指示
        ExecuteSkillUseClientRpc(skillIndex);
    }

    [ClientRpc]
    private void ExecuteSkillUseClientRpc(int skillIndex)
    {
        if (IsOwner) return; // 自分は実行済みなので無視

        player.UseCurrentSkill(); // スキルのアクションを呼び出す

        inputController.ExecuteSkillUseLocal();
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient && !IsServer)
        {
            netActiveEffects.OnListChanged -= HandleNetworkListChanged;
        }
    }

    private void HandleNetworkListChanged(NetworkListEvent<NetworkEffectData> changeEvent)
    {
        RebuildEffectsFromNetworkList();
    }

    private void RebuildEffectsFromNetworkList()
    {
        var newList = new List<EffectAbility>();

        foreach (var netData in netActiveEffects)
        {
            Effect effectSO = EffectRegistry.Get(netData.EffectType, netData.IsUp);

            if (effectSO != null)
            {
                newList.Add(new EffectAbility(
                    effectSO,
                    netData.IsDisplay,
                    netData.Time,
                    netData.Value
                ));
            }
        }

        // クライアント側のRoot(ReactiveProperty)を更新
        root.ActiveEffects.Value = newList;
    }
}