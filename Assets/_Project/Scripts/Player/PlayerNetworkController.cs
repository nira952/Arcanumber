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

    private Rigidbody2D rigidbody2D;

    private readonly NetworkVariable<int> netPlayerIndex = new(-1);
    private readonly NetworkVariable<int> netCurrentHealth = new(100);
    private readonly NetworkVariable<bool> netIsDown = new(false);
    private readonly NetworkList<NetworkEffectData> netActiveEffects = new NetworkList<NetworkEffectData>();

    // 入力受付用（ローカル操作者専用）
    public bool CanProcessInput
    {
        get
        {
            if (!IsSpawned || !IsOwner) return false;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState.Value != GameState.Playing) return false;
            return !root.IsDown.Value;
        }
    }

    private void Awake()
    {
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
        // 所有者でない場合、Inputコンポーネントを停止
        if (!IsOwner)
        {
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;
            root.gameObject.tag = "Enemy";
        }

        Debug.Log($"[PlayerNetworkController] OnNetworkSpawn - IsOwner: {IsOwner}");

        if (IsServer)
        {
            int assignedIndex = PlayerDataManager.Instance.GetLobbyIndexByClientId(OwnerClientId);
            root.PlayerIndex.Value = assignedIndex;

            root.PlayerIndex.Subscribe(v => netPlayerIndex.Value = v).AddTo(this);
            root.CurrentHealth.Subscribe(v => netCurrentHealth.Value = v).AddTo(this);

            // 【修正箇所 1】サーバー側で IsDown が変更されたら NetworkVariable に同期し、勝敗判定を行う
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

            Observable.FromEvent<NetworkVariable<int>.OnValueChangedDelegate, int>(
                h => (oldV, newV) => h(newV),
                h => netCurrentHealth.OnValueChanged += h,
                h => netCurrentHealth.OnValueChanged -= h
            ).Prepend(netCurrentHealth.Value).Subscribe(v => root.CurrentHealth.Value = v).AddTo(this);

            // 【修正箇所 2】クライアント側は root.IsDown の同期のみを行う
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

        
        root.Initialize(this);
    }


    private void InitializeUI(int index)
    {
        if (GameUIManager.Instance == null) return;
        
        string playerName = PlayerDataManager.Instance.GetPlayerNameByIndex(index);

        GameUIManager.Instance.SetPlayerName(index, playerName);
        GameUIManager.Instance.SetHealthSliderMaxValue(index, 100);
    }

    // --- ダメージ処理の要求 ---
    public void RequestTakeDamage(int damage)
    {
        if (IsServer) root.ApplyDamage(damage); // サーバーなら直接処理
        else TakeDamageServerRpc(damage);       // クライアントならサーバーへ要請
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(int damage) => root.ApplyDamage(damage);

    // --- 各アクション処理 (前回の実装まま) ---
    public void RequestJump()
    {
        root.GetActionController().ExecuteJumpLocal();
        RequestJumpServerRpc();
    }

    [ServerRpc] private void RequestJumpServerRpc() => ExecuteJumpClientRpc();
    [ClientRpc] private void ExecuteJumpClientRpc() { if (!IsOwner) root.GetActionController().ExecuteJumpLocal(); }

    public void RequestAttack()
    {
        root.GetActionController().ExecuteAttackLocal();
        player.UseAttack(); // 攻撃のアクションを呼び出す
        RequestAttackServerRpc();
    }
    [ServerRpc] private void RequestAttackServerRpc() => ExecuteAttackClientRpc();
    [ClientRpc] private void ExecuteAttackClientRpc() 
    { 
        if (!IsOwner)
        {
            root.GetActionController().ExecuteAttackLocal();
            player.UseAttack(); // 攻撃のアクションを呼び出す
        }

    }

    public void RequestSkillSelect(int direction) => root.GetActionController().ExecuteSkillSelectLocal(direction);

    public void RequestSkillUse()
    {
        player.UseCurrentSkill(); // スキルのアクションを呼び出す

        //// 2. サーバーへスキルの発動を要求
        RequestSkillUseServerRpc(root.SelectedSkillIndex.Value);
    }

    [ServerRpc]
    private void RequestSkillUseServerRpc(int skillIndex)
    {
        // サーバー側での当たり判定やダメージ計算
        // SkillLogic.Execute(root, root.SkillList[skillIndex]); など

        // 他クライアントへ演出の再生を指示
        ExecuteSkillUseClientRpc(skillIndex);
    }

    [ClientRpc]
    private void ExecuteSkillUseClientRpc(int skillIndex)
    {
        if (IsOwner) return; // 自分は実行済みなので無視

        player.UseCurrentSkill(); // スキルのアクションを呼び出す

        root.GetActionController().ExecuteSkillUseLocal();
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
            // ★ 作成いただいた EffectRegistry.Get を呼び出してSOを取得！
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