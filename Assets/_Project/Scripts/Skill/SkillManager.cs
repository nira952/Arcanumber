using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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
    public void RequestSkill(PlayerRoot player,int skillIndex)
    {
        if (player == null) return;

        Vector2 aimPos = player.GetAimCursor().GetTransform().position;


        // オフライン時、またはすでにサーバー上で動作している場合は直接実行
        if (isLocalMode || IsServer)
        {
            Skill skill = player.GetCurrentSkill();

            StartCoroutine(SkillSpawnDelayCoroutine(player, skill, aimPos));
        }
        else
        {
            // クライアントからの要求の場合：ServerRpc を経由してサーバー側で処理を開始する
            // ※必要であれば、ここでクライアントローカルの予兆エフェクト（ローカル先行表示）を再生する
            RequestSkillServerRpc(player.PlayerIndex.Value, skillIndex, aimPos);
        }
    }



    [ServerRpc(RequireOwnership = false)]
    private void RequestSkillServerRpc(int playerIndex,int skillIndex ,Vector2 aimPos)
    {
        // PlayerIndex からそのプレイヤーの PlayerRoot を取得する
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(playerIndex);
        // SkillIndex からそのスキルを取得する
        Skill skill = player.GetSkill()[skillIndex];

        if (player != null && skill != null)
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
        if (reductionValue <= 0f) reductionValue = 1f; // 0以下なら倍率1（短縮なし）とする

        float delayTime = activeSkill.GetDelayTime() / reductionValue;

        // 2. 予備動作（予兆）エフェクト生成
        GameObject activePreview = MagicStartSpawn(player, activeSkill, pos, delayTime);

        // 3. 待機時間の計算（負の値にならないよう Mathf.Max でガード）
        float waitTime = Mathf.Max(0f, delayTime - 0.1f);
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        // 予備動作オブジェクトがあればその位置、なければエイム位置をスポーン位置とする
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
        }

        // 4. オンライン時のネットワークスポーン同期
        if (!isLocalMode && IsServer)
        {
            if (skillObj.TryGetComponent(out NetworkObject networkObject))
            {
                // サーバー上で Spawn を呼ぶことで、全クライアントの画面へ一斉に同期生成される
                networkObject.Spawn();
            }
        }
    }

    // 対象プレイヤーを回復する（サーバー側で実行）
    private void Heal(PlayerRoot playerRoot, Skill skill)
    {
        float baseHealAmount = skill.GetAtk();

        // 1. 自分自身を回復
        playerRoot.ApplyHeal(baseHealAmount);
        AnimationInstance(playerRoot, skill);

        // 2. HealStealを持っている場合、半分回復を適用
        float sharedHealAmount = baseHealAmount * 0.5f;

        List<PlayerRoot> targets = PlayerUtility.GetAllPlayer();

        foreach (var target in targets)
        {
            if (target == null) continue;

            // HealSteal効果を持っている場合に回復を適用
            if (target != playerRoot && target.HasEffect(EffectList.HealSteal,true))
            {
                target.ApplyHeal(sharedHealAmount);

                // 回復を受けたエフェクトを再生
                AnimationInstance(target, skill);
            }
        }
    }
    private void EffectBuffPlayer(PlayerRoot playerRoot, Skill skill)
    {
        playerRoot.AddEffect(skill.GetEffect().Clone());
        AnimationInstance(playerRoot, skill);
    }

    private void AnimationInstance(PlayerRoot playerRoot, Skill skill)
    {
        if (skill.GetEffectAnimation() == null) return;

        GameObject animObj = Instantiate(skill.GetEffectAnimation(), playerRoot.transform);

        // クライアント側へもアニメーションエフェクトを同期したい場合（NetworkObjectがない演出用プレハブの場合）
        if (!isLocalMode && IsServer)
        {
            if (animObj.TryGetComponent(out NetworkObject netObj))
            {
                netObj.Spawn();
            }
        }
    }
}