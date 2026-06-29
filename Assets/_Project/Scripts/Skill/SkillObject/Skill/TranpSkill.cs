using UnityEngine;

public class TranpSkill : MagicObject
{
    // 通常の変数に変更
    private int syncedDmg = 0;

    [SerializeField] private GameObject cardObj;
    [SerializeField] private Sprite[] cardSprites;

    public override void Initialize(int charaNo, Skill skill, Vector2 pos)
    {
        base.Initialize(charaNo, skill, pos);

        SetMovement(false, 0f, false, true);
        SetupPositionAndRotation(pos);

        // ネットワーク制御を外したため、ここで直接実行
        RollTranp();
    }

    void RollTranp()
    {
        // 運があるかどうか
        bool hasLuck = PlayerUtility.HaveEffect(PlayerUtility.FindPlayerByNo(haveCharaNo), EffectList.Lacky, true);

        // 確率テーブル
        int[] weights = hasLuck
            ? new int[] { 0, 1, 1, 3, 25, 70 }
            : new int[] { 80, 15, 3, 1, 1, 0 };

        int totalWeight = 0;
        foreach (int w in weights) totalWeight += w;

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        int resultIndex = 0;

        for (int j = 0; j < weights.Length; j++)
        {
            currentWeight += weights[j];
            if (randomValue < currentWeight)
            {
                resultIndex = j;
                break;
            }
        }

        // 変数にセット
        syncedDmg = resultIndex;

        // セット完了後、カードの更新処理を呼ぶ
        UpdateCard();
    }

    void UpdateCard()
    {
        if (syncedDmg < 0)
        {
            Destroy(gameObject);
            return;
        }

        // 生成先を取得
        var player = PlayerUtility.FindPlayerByNo(haveCharaNo);
        if (player == null) return;
        Transform parentTransform = player.GetPlayerController().GetAimCursor().GetEfeUpperPos().transform;

        // インスタンス化
        GameObject card = Instantiate(cardObj, parentTransform);

        // SpriteRendererを取得
        SpriteRenderer sr = card.GetComponent<SpriteRenderer>();
        if (sr != null && syncedDmg > 0 && syncedDmg < cardSprites.Length + 1) // インデックス範囲修正
        {
            sr.sprite = cardSprites[syncedDmg - 1];
        }
        
　      Destroy(card, 1.0f);
    }

    protected override void OnHit(NetworkPlayer target)
    {
        NetworkPlayer player = PlayerUtility.FindPlayerByNo(haveCharaNo);
        //ダメージを与える
        PlayerUtility.FinalDamage(target, player, dmg);
        // エフェクトをつける
        if (effect != null)
            target.SetHaveEffect(effect.Clone());
    }
}