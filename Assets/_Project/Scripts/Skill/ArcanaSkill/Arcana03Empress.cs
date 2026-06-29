// ========================================================
// 女帝：Empress
// ========================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// 女帝（正位置）
/// </summary>
public class Arcana03EmpressFront : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //自然回復
    public const int healValue = 1;
    public const float timeInterval = 5f;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana) { player.StartCoroutine(OnUpdate(player, sourceArcana)); }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        while (true)
        {
            yield return new WaitForSeconds(timeInterval);
            PlayerUtility.FinalHeal(player, healValue);
        }
    }
}

/// <summary>
/// 女帝（逆位置）
/// </summary>
public class Arcana03EmpressBack : ArcanaLogic
{
    public override ASkillCategory GetCategory() => ASkillCategory.StartEffect;
    //ランダムな効果
    public const float timeInterval = 20f;
    public EffectAbility randomEffect;
    public override void Execute(NetworkPlayer player, Arcana sourceArcana) { player.StartCoroutine(OnUpdate(player, sourceArcana)); }
    public override IEnumerator OnUpdate(NetworkPlayer player, Arcana sourceArcana)
    {
        Transform pos = player.GetPlayerController().GetAimCursor().GetEfeUpperPos();
        while (true)
        {
            PlayArcanaVisuals(player, sourceArcana, pos);
            yield return new WaitForSeconds(GetVisualDuration(sourceArcana));

            //6割の確率でバフ、4割の確率でデバフ
            bool isSuccess = Random.value < 0.6f;
            //数値をランダムに返す（偏りあり）
            float biasedRandom = 1f - Mathf.Sqrt(Random.value);
            float finalNum = (isSuccess) ? Mathf.Lerp(0f, 3f, biasedRandom) : Mathf.Lerp(0f, 0.4f, biasedRandom);
            //ランダムな効果の種類を選ぶ
            EffectList randomType = (EffectList)Random.Range(0, 3);
            Effect masterEffect = EffectRegistry.Get(randomType, isSuccess);

            //ランダムな効果を選ぶ
            if (masterEffect != null)
            {
                //レジストリから得たマスターデータを元に、能力インスタンスを生成
                randomEffect = new EffectAbility(masterEffect, true, timeInterval, finalNum);
                player.SetHaveEffect(randomEffect.Clone()); 
            }

            yield return new WaitForSeconds(timeInterval - GetVisualDuration(sourceArcana));
        }
    }
}
