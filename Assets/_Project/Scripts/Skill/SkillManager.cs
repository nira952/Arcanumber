using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using R3;
using System.Linq;

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

    [SerializeField] private Bullet bulletPrefab; // 弾丸のプレハブ

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
            Skill skill = player.GetCurrentSkill();

            StartCoroutine(SkillSpawnDelayCoroutine(player, skill, aimPos));

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
        if (skills == null || skillIndex < 0 || skillIndex >= skills.Length) return;

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
        if (skill.GetAimSelect() == AimSelect.LookOn && player.GetMagicStart() != null)
        {
            GameObject mStart = Instantiate(player.GetMagicStart().gameObject, pos, Quaternion.identity);

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
        if (skill.GetEffectAnimation() == null)
        {
            Debug.LogWarning("エフェクトが見つかりません。");
            return;
        }

        int bulletCount = skill.GetBulletCount();
        if (bulletCount <= 0) bulletCount = 1;  //0個以下の場合は1個に

        float totalSpreadAngle = skill.GetSpreadAngle(); //例: 60度（上下に広げる全体の角度）
        bool isRandomAngle = totalSpreadAngle < 0f;     //マイナスならランダム判定
        float absAngle = Mathf.Abs(totalSpreadAngle);

        //プレイヤーからマウス（AIM）の位置への方向を計算し、基準の角度（baseAngle）を求める
        Vector2 aimDirection = (pos - (Vector2)playerRoot.transform.position).normalized;
        float baseAngle = aimDirection != Vector2.zero ? Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg : 0f;

        for (int i = 0; i < bulletCount; i++)
        {
            //--- 角度の計算 ---
            float currentAngle = 0f;

            if (bulletCount <= 1)
                currentAngle = 0f; //0個または1個の場合は正面（0度）
            else
            {
                if (isRandomAngle)
                    //マイナス指定：指定された範囲（absAngle）の中でランダムに散らす
                    currentAngle = Random.Range(-absAngle * 0.5f, absAngle * 0.5f);
                else
                {
                    //プラス指定：決められた範囲（absAngle）の中で上下に等間隔で配置する
                    //i = 0 のとき一番下、i = max-1 のとき一番上になるように綺麗に割り振る
                    float halfAngle = absAngle * 0.5f;
                    if (bulletCount == 1)
                        currentAngle = 0f;
                    else
                    {
                        //端から端までを均等割り
                        float step = absAngle / (bulletCount - 1);
                        currentAngle = -halfAngle + (step * i);
                    }
                }
            }

            //プレイヤーの向きではなく、「マウスの方向（baseAngle）」をベースにオフセット角（currentAngle）を加算する
            Quaternion spawnRotation = Quaternion.Euler(0, 0, baseAngle + currentAngle);

            Vector2 spawnPos = playerRoot.transform.position;
            if (isRandomAngle)
            {
                // 半径 0.4f 以内のランダムな位置にズラす（数値はお好みで調整してください）
                Vector2 randomOffset = Random.insideUnitCircle * 0.4f;
                spawnPos += randomOffset;
            }

            //サーバー（またはオフライン）側でオブジェクトを Instantiate 生成
            GameObject skillObj = Instantiate(skill.GetEffectAnimation(), playerRoot.transform.position, spawnRotation);

            //サイズ変更効果の適用
            float sizeMultiplier = playerRoot.GetEffectValue(EffectList.SizeChange);
            if (sizeMultiplier > 0f)
                skillObj.transform.localScale *= sizeMultiplier;

            //スキルコンポーネントの初期化
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

                //もしkeepTimeが0以下の場合は、消さない
                if (keepTime > 0f)
                    Destroy(skillObj, keepTime);
            }

            //オンライン時のネットワークスポーン同期
            if (!isLocalMode && IsServer)
            {
                if (skillObj.TryGetComponent(out NetworkObject networkObject))
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



    public void SpawnRpcObject(int playerIndex)
    {
        if (isLocalMode || IsServer)
        {
            PlayerRoot player = PlayerUtility.GetPlayerByIndex(playerIndex);
            GameObject prefab = player.GetArcana().GetEffectPrefab();

            // オフラインモードまたはサーバー側で直接生成
            RequestSpawnRpcObject(player, prefab);
        }
        else
        {
            // クライアントからの要求の場合：ServerRpc を経由してサーバー側で処理を開始する
            RequestSpawnRpcObjectServerRpc(playerIndex);
        }

    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnRpcObjectServerRpc(int playerIndex)
    {
        // 1. 安全に PlayerRoot を取得（Nullチェック）
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(playerIndex);
        if (player == null) return;

        // ここでプレイヤーのArcanaからエフェクトプレハブを取得
        GameObject prefab = player.GetArcana().GetEffectPrefab();

        // 2. サーバー側でオブジェクトを生成し、全クライアントに同期させる
        RequestSpawnRpcObject(player, prefab);
    }


    private void RequestSpawnRpcObject(PlayerRoot player,GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Arcanaのエフェクトプレハブが設定されていません。");
            return;
        }

        // オブジェクトを生成する
        GameObject obj = Instantiate(prefab, player.transform.position, Quaternion.identity);

        // インターフェースを持つコンポーネントを取得して初期化
        if (obj.TryGetComponent(out IRpcObjectInterface rpcInterface))
        {
            rpcInterface.RpcInitialize(player.PlayerIndex.Value);

            rpcInterface.OnDestroyed.Subscribe(_ =>
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }).AddTo(obj);

        }

        // ネットワークオブジェクトの場合は全クライアントに同期スポーンする
        if (!isLocalMode && IsServer)
        {
            if (obj.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn();
            }
        }

    }

    public void SpawnBulletObject(int playerIndex,Vector2 pos,Vector2 direction)
    {
        if (isLocalMode || IsServer)
        {
            RequestSpawnBullet(playerIndex, pos, direction);
        }
        else
        {
            RequestSpawnBulletServerRpc(playerIndex, pos, direction);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnBulletServerRpc(int playerIndex, Vector2 pos, Vector2 direction)
    {
        // 2. サーバー側でオブジェクトを生成し、全クライアントに同期させる
        RequestSpawnBullet(playerIndex, pos, direction);
    }

    private void RequestSpawnBullet(int index, Vector2 pos, Vector2 direction)
    {
        // オブジェクトを生成する
        Bullet bullet = Instantiate(bulletPrefab, pos, Quaternion.identity);

        bullet.BulletInitialize(index, pos);

        // インターフェースを持つコンポーネントを取得して初期化
        if (bullet.TryGetComponent(out IRpcObjectInterface rpcInterface))
        {
            rpcInterface.OnDestroyed.Subscribe(_ =>
            {
                if (bullet != null)
                {
                    Destroy(bullet.gameObject);
                }
            }).AddTo(bullet.gameObject);
        }
        // ネットワークオブジェクトの場合は全クライアントに同期スポーンする
        if (!isLocalMode && IsServer)
        {
            if (bullet.gameObject.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn();
            }
        }
    }


    /// <summary>
    /// スプライトが有効な場合のみオブジェクトを生成し、スプライトを変更し、指定秒数後に削除するメソッド
    /// </summary>
    public void SpawnChangeAndDestroy(GameObject obj, Transform part, Sprite sprite, float destroyTime)
    {
        // 生成
        GameObject gameObject = Instantiate(obj, part);
        if (gameObject.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.Spawn();
        }

        // スプライト変更
        if (gameObject.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.sprite = sprite;
        }

        // もしスプライトが null なら処理を飛ばす（生成しない）
        if (sprite == null)
            return;

        // 指定秒数後に削除
        Destroy(gameObject, destroyTime);
    }
}