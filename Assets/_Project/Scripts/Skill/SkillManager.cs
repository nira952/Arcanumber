using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using R3;

/// <summary>
/// スキルの生成と管理を行うクラス
/// </summary>
public class SkillManager : NetworkBehaviour
{
    #region Singleton
    public static SkillManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // オフラインモードかどうかを判定
        isLocalMode = PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsLocalMode;
    }
    #endregion

    private bool isLocalMode = false;

    /// <summary>
    /// スキルの使用リクエストを受け付ける（外部からの入口）
    /// </summary>
    public void RequestSkill(PlayerRoot player)
    {
        if (player == null) return;

        Vector2 aimPos = player.GetAimCursor().GetTransform().position;
        int skillIndex = player.SelectedSkillIndex.Value;

        // オフライン時、またはすでにサーバー上で動作している場合は直接実行
        if (isLocalMode || IsServer)
        {
            var skills = player.GetSkill();
            if (skills != null && skillIndex >= 0 && skillIndex < skills.Count)
            {
                Skill skill = skills[skillIndex];
                StartCoroutine(SkillSpawnDelayCoroutine(player, skill, aimPos));
            }
        }
        else
        {
            // クライアントからの要求の場合：ServerRpc を経由してサーバー側で処理を開始する
            RequestSkillServerRpc(player.PlayerIndex.Value, skillIndex, aimPos);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSkillServerRpc(int playerIndex, int skillIndex, Vector2 aimPos)
    {
        // 1. 安全に PlayerRoot を取得（Nullチェック）
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(playerIndex);
        if (player == null) return;

        // 2. インデックスの範囲チェックを行ってから Skill を取得
        var skills = player.GetSkill();
        if (skills == null || skillIndex < 0 || skillIndex >= skills.Count) return;

        Skill skill = skills[skillIndex];
        if (skill != null)
        {
            StartCoroutine(SkillSpawnDelayCoroutine(player, skill, aimPos));
        }
    }

    /// <summary>
    /// スキル生成までの待機コルーチン（サーバーまたはローカルで実行される）
    /// </summary>
    private IEnumerator SkillSpawnDelayCoroutine(PlayerRoot player, Skill activeSkill, Vector2 pos)
    {
        // 1. スキル短縮効果の計算（ゼロ除算防止）
        float reductionValue = player.GetEffectValue(EffectList.SkillTimeReduction);
        if (reductionValue <= 0f) reductionValue = 1f;

        float delayTime = activeSkill.GetDelayTime() / reductionValue;

        // 2. 予備動作（予兆）エフェクト生成
        GameObject activePreview = MagicStartSpawn(player, activeSkill, pos, delayTime);

        // 3. 待機時間の計算（負の値にならないよう Mathf.Max でガード）
        float waitTime = Mathf.Max(0f, delayTime - 0.1f);
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        Vector2 finalSpawnPos = activePreview != null ? (Vector2)activePreview.transform.position : pos;

        // 4. スキルの本生成
        SkillSpawn(player, activeSkill, finalSpawnPos);
    }

    /// <summary>
    /// 魔法の予備動作（予兆）を出すメソッド
    /// </summary>
    private GameObject MagicStartSpawn(PlayerRoot player, Skill skill, Vector2 pos, float delayTime)
    {
        if (skill.GetAimSelect() == AimSelect.LookOn && player.GetMagic() != null)
        {
            GameObject mStart = Instantiate(player.GetMagic().gameObject, pos, Quaternion.identity);

            // ネットワークオブジェクトの場合は全クライアントに同期スポーンする
            if (!isLocalMode && IsServer)
            {
                if (mStart.TryGetComponent(out NetworkObject netObj))
                {
                    netObj.Spawn();
                }
            }

            Destroy(mStart, delayTime);
            return mStart;
        }
        return null;
    }

    /// <summary>
    /// スキルの分類別スポーン処理
    /// </summary>
    public void SkillSpawn(PlayerRoot player, Skill skill, Vector2 pos)
    {
        switch (skill.GetSkillCategory())
        {
            case SkillCategory.Heal:
                Heal(player, skill);
                break;
            case SkillCategory.Attack:
                SkillObjectSpawn(player, skill, pos);
                break;
            case SkillCategory.Effection:
                EffectBuffPlayer(player, skill);
                break;
            default:
                Debug.LogWarning($"未対応のスキルカテゴリです: {skill.GetSkillCategory()}");
                break;
        }
    }

    /// <summary>
    /// 攻撃型スキルオブジェクトの生成とネットワーク同期
    /// </summary>
    private void SkillObjectSpawn(PlayerRoot playerRoot, Skill skill, Vector2 pos)
    {
        if (skill.GetEffectAnimation() == null) return;

        // 1. サーバー（またはオフライン）側でオブジェクトを Instantiate 生成
        GameObject skillObj = Instantiate(skill.GetEffectAnimation(), playerRoot.transform.position, Quaternion.identity);

        // 2. サイズ変更効果の適用
        float sizeMultiplier = playerRoot.GetEffectValue(EffectList.SizeChange);
        if (sizeMultiplier > 0f)
        {
            skillObj.transform.localScale *= sizeMultiplier;
        }

        // 3. スキルコンポーネントの初期化
        if (skillObj.TryGetComponent(out SkillObject magic))
        {
            magic.Initialize(playerRoot.PlayerIndex.Value, skill, pos);

            magic.OnDestroyed
                .Subscribe(_ =>
                {
                    if (skillObj != null) Destroy(skillObj);
                })
                .AddTo(skillObj);

            float keepTime = skill.GetKeepTime();
            Destroy(skillObj, keepTime);
        }

        // 4. オンライン時のネットワークスポーン同期
        if (!isLocalMode && IsServer)
        {
            if (skillObj.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn();
            }
        }
    }

    private void Heal(PlayerRoot playerRoot, Skill skill)
    {
        float baseHealAmount = skill.GetAtk();

        playerRoot.ApplyHeal(baseHealAmount);
        PlayAnimation(playerRoot, skill);

        float sharedHealAmount = baseHealAmount * 0.5f;
        List<PlayerRoot> targets = PlayerUtility.GetAllPlayer();

        foreach (var target in targets)
        {
            if (target == null) continue;

            if (target != playerRoot && target.HaveEffect(EffectList.HealSteal, true))
            {
                target.ApplyHeal(sharedHealAmount);
                PlayAnimation(target, skill);
            }
        }
    }

    private void EffectBuffPlayer(PlayerRoot playerRoot, Skill skill)
    {
        playerRoot.AddEffect(skill.GetEffect().Clone());
        PlayAnimation(playerRoot, skill);
    }

    /// <summary>
    /// 【サーバー専用】演出再生の呼び出し口
    /// </summary>
    private void PlayAnimation(PlayerRoot playerRoot, Skill skill)
    {
        if (skill.GetEffectAnimation() == null) return;

        // ローカル（オフライン）実行時
        if (isLocalMode)
        {
            SpawnLocalAnimation(playerRoot, skill.GetEffectAnimation());
            return;
        }

        // サーバー（オンライン）実行時：全クライアントへ再生命令を送信
        if (IsServer)
        {
            // スキルIDやプレイヤーIndexを渡して全クライアントで一斉再生させる
            PlayAnimationClientRpc(playerRoot.PlayerIndex.Value, skill.GetSkillNo());
        }
    }

    /// <summary>
    /// 【全クライアントで実行】演出プレハブをローカル生成する ClientRpc
    /// </summary>
    [ClientRpc]
    private void PlayAnimationClientRpc(int targetPlayerIndex, int skillId)
    {
        // 1. 対象のプレイヤーを取得
        PlayerRoot targetPlayer = PlayerUtility.GetPlayerByIndex(targetPlayerIndex);
        if (targetPlayer == null) return;

        // 2. スキル情報またはデータベースから対応するエフェクトプレハブを取得
        Skill skill = AssetLoader.Instance.GetSkill(skillId);
        if (skill == null || skill.GetEffectAnimation() == null) return;

        // 3. ローカル上で生成（NetworkObjectを持たない純粋なGameObject）
        SpawnLocalAnimation(targetPlayer, skill.GetEffectAnimation());
    }

    /// <summary>
    /// 実際の Instantiate と Destroy 処理（ローカル専用）
    /// </summary>
    private void SpawnLocalAnimation(PlayerRoot targetPlayer, GameObject animPrefab)
    {
        // プレイヤーの位置に生成（必要に応じて親に設定）
        GameObject animObj = Instantiate(animPrefab, targetPlayer.transform.position, Quaternion.identity, targetPlayer.transform);

        // パーティクルシステムがアタッチされている場合は長さを取得して自動破棄
        float destroyTime = 2.0f; // デフォルト生存時間
        if (animObj.TryGetComponent(out ParticleSystem ps))
        {
            destroyTime = ps.main.duration + ps.main.startLifetime.constantMax;
        }

        // 各クライアントの画面上で独立して破棄される（ネットワーク通信不要）
        Destroy(animObj, destroyTime);
    }
}