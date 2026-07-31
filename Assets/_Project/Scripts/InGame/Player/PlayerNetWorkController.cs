//using NUnit.Framework;
//using R3;
//using System.Collections.Generic;
//using Unity.Netcode;
//using UnityEngine;

//namespace nira.Demo
//{
//    [RequireComponent(typeof(DemoPlayer))]
//    public class PlayerNetworkController : NetworkBehaviour, IPlayerActionHandler
//    {
//        private DemoPlayer player;

//        // ネットワーク同期用の変数はここに持たせる
//        private readonly NetworkVariable<int> netPlayerIndex = new(-1);
//        private readonly NetworkVariable<int> netCurrentHealth = new(100);
//        private readonly NetworkVariable<bool> netIsDown = new(false);
//        private readonly NetworkVariable<List<EffectAbility>> netActiveEffects = new(new List<EffectAbility>());

//        public bool CanProcessInput => IsSpawned && IsOwner && !player.IsDown.Value;

//        private DemoSkill skill;

//        private void Awake()
//        {
//            player = GetComponent<DemoPlayer>();
//        }

//        public override void OnNetworkSpawn()
//        {
//            if (IsServer)
//            {
//                // サーバー側：DemoPlayerのデータが書き換わったら、NetworkVariableに同期する
//                player.PlayerIndex.Subscribe(v => netPlayerIndex.Value = v).AddTo(this);
//                player.CurrentHealth.Subscribe(v => netCurrentHealth.Value = v).AddTo(this);
//                player.IsDown.Subscribe(v => netIsDown.Value = v).AddTo(this);

//            }
//            else
//            {
//                // クライアント側：ネットワーク経由でデータが届いたら、DemoPlayer（ローカルデータ）に反映する
//                Observable.FromEvent<NetworkVariable<int>.OnValueChangedDelegate, int>(
//                    h => (oldV, newV) => h(newV),
//                    h => netPlayerIndex.OnValueChanged += h,
//                    h => netPlayerIndex.OnValueChanged -= h
//                ).Prepend(netPlayerIndex.Value).Subscribe(v => player.PlayerIndex.Value = v).AddTo(this);

//                Observable.FromEvent<NetworkVariable<int>.OnValueChangedDelegate, int>(
//                    h => (oldV, newV) => h(newV),
//                    h => netCurrentHealth.OnValueChanged += h,
//                    h => netCurrentHealth.OnValueChanged -= h
//                ).Prepend(netCurrentHealth.Value).Subscribe(v => player.CurrentHealth.Value = v).AddTo(this);

//                Observable.FromEvent<NetworkVariable<bool>.OnValueChangedDelegate, bool>(
//                    h => (oldV, newV) => h(newV),
//                    h => netIsDown.OnValueChanged += h,
//                    h => netIsDown.OnValueChanged -= h
//                ).Prepend(netIsDown.Value).Subscribe(v => player.IsDown.Value = v).AddTo(this);
//            }

//            // UIの更新は、DemoPlayer側のReactivePropertyを監視すれば、オン/オフライン問わず共通化できる！
//            player.PlayerIndex.Where(idx => idx != -1).Take(1).Subscribe(idx => InitializeUI(idx)).AddTo(this);
//            player.CurrentHealth.Subscribe(hp => {
//                if (player.PlayerIndex.Value != -1 && GameUIManager.Instance != null)
//                {
//                    GameUIManager.Instance.UpdateHealth(player.PlayerIndex.Value, hp);
//                }
//            }).AddTo(this);
//        }

//        private void InitializeUI(int index)
//        {
//            if (GameUIManager.Instance == null) return;

//            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.TryGetPlayerData(index, out var myData))
//            {
//                GameUIManager.Instance.SetPlayerName(index, myData.PlayerName.ToString());
//            }
//            else
//            {
//                GameUIManager.Instance.SetPlayerName(index, $"Player {index}");
//            }
//            GameUIManager.Instance.SetHealthSliderMaxValue(index, 100);
//        }

//        // --- クライアントからの命令をサーバーへ届ける窓口 ---
//        public void RequestTakeDamage(int damage)
//        {
//            if (IsServer) player.ApplyDamage(damage);
//            else TakeDamageServerRpc(damage);
//        }

//        [ServerRpc]
//        private void TakeDamageServerRpc(int damage) => player.ApplyDamage(damage);


//        public void RequestActivateSkill()
//        {
//            if (IsServer)
//            {
//                // 自分がホストなら直接Rpcを叩く
//                ActivateSkillServerRpc();
//            }
//            else
//            {
//                // クライアントならサーバーに要請する
//                ActivateSkillServerRpc();
//            }
//        }

//        [ServerRpc]
//        private void ActivateSkillServerRpc()
//        {
//            // ① サーバー側でダメージ計算などのロジックを実行
//            skill.ExecuteLogicServerOnly(GetComponent<DemoPlayer>().PlayerIndex.Value);

//            // ② 全クライアントに対して「エフェクトを再生しろ」という命令をブロードキャストする
//            PlaySkillEffectClientRpc();
//        }

//        [ClientRpc]
//        private void PlaySkillEffectClientRpc()
//        {
//            // ③ 命令を受け取った全クライアント（自分を含む）の画面でエフェクトが再生される
//            skill.PlayVisualEffect();
//        }
//    }
//}
