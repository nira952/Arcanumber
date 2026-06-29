using System.Collections;
using Unity.Services.Lobbies.Models;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// スキルの生成と管理を行うクラス
/// </summary>
public class SkillManager : SingletonMonoBehaviour<SkillManager>
{ 
    /// <summary>
    /// GameManagerから一番最初に呼ばれる受付窓口
    /// </summary>
    public void RequestSkill(NetworkPlayer player)
    {
        //ボタンが押されたスキルを確定
        Skill skill = player.GetNoSkill();
        if (skill == null) return;

        //ポジションも確定
        Vector2 pos = player.GetPlayerController().GetAimCursor().GetTransform().position;

        //ディレイのコルーチンを開始
        StartCoroutine(SkillSpawnDelayCoroutine(player, skill, pos));
    }
    public void RequestSkill(NetworkPlayer player, int skillNo)
    {
        //ボタンが押されたスキルを確定
        Skill skill = player.GetSkill()[skillNo];
        if (skill == null) return;

        //ポジションも確定
        Vector2 pos = player.GetPlayerController().GetAimCursor().GetTransform().position;

        //ディレイのコルーチンを開始
        StartCoroutine(SkillSpawnDelayCoroutine(player, skill, pos));
    }


    /// <summary>
    /// 時間を待つためのコルーチン
    /// </summary>
    private IEnumerator SkillSpawnDelayCoroutine(NetworkPlayer player, Skill skill, Vector2 pos)
    {
        float delayTime = skill.GetDelayTime() / PlayerUtility.GetEffectValue(player, EffectList.SkillTimeReduction);
        //生成された予備動作オブジェクトを一度変数にキープする
        GameObject activePreview = MagicStartSpawn(player, skill, pos, delayTime);

        //待機時間
        yield return new WaitForSeconds(delayTime - 0.1f);

        //ここで場所を決める
        Vector2 finalSpawnPos = pos;
        //もし予備動作オブジェクトが生成されていたら、その位置を最終的なスポーン位置とする
        if (activePreview != null)
            finalSpawnPos = activePreview.transform.position;

        //確定した最新のポジションを渡してスポーン！
        SkillSpawn(player, skill, finalSpawnPos);
    }

    /// <summary>
    /// 魔法の予備動作を出すメソッド
    /// </summary>
    GameObject MagicStartSpawn(NetworkPlayer player, Skill skill, Vector2 pos, float delayTime)
    {
        if (skill.GetAimSelect() == AimSelect.LockOn)
        {
            GameObject mStart = Instantiate(player.GetMagicStart());
            mStart.transform.position = pos;
            Destroy(mStart, delayTime);

            return mStart;
        }
        return null;
    }

    /// <summary>
    /// スキルのスポーンメソッド
    /// </summary>
    public void SkillSpawn(NetworkPlayer player, Skill skill, Vector2 pos)
    {
        switch (skill.GetSkillCategory())
        {
            case SkillCategory.Heal:
                Heal(player, skill);
                break;
            case SkillCategory.Attack:
                SkillObjectSpawn(player, skill, pos);
                break;
            case SkillCategory.EffectionBuff:
                EffectBuffPlayer(player, skill);
                break;
            default:
                Debug.Log("未対応のカテゴリです");
                break;
        }
    }

    /// <summary>
    /// 攻撃スキルの場合
    /// </summary>
    void SkillObjectSpawn(NetworkPlayer player, Skill skill, Vector2 pos)
    {
        //スキルのオブジェクトを生成
        GameObject skillObj = Instantiate(skill.GetEffectAnimation());
        //サイズ変更のエフェクトがある場合は、スキルオブジェクトのサイズを変更する
        skillObj.transform.localScale *=
            PlayerUtility.GetEffectValue(player, EffectList.SizeChange);
        //生成したスキルオブジェクトの位置をプレイヤーの位置にする
        skillObj.transform.position = player.gameObject.transform.position;
        //スクリプトをアタッチする
        MagicObject magic = skillObj.GetComponent<MagicObject>();
        magic.Initialize(player.GetNetworkId(), skill, pos);
    }

    /// <summary>
    /// 回復スキルの場合
    /// </summary>
    void Heal(NetworkPlayer player, Skill skill)
    {
        PlayerUtility.FinalHeal(player, skill.GetAtk());
        AnimationInstance(player, skill);
    }

    /// <summary>
    /// エフェクト付与の場合
    /// </summary>
    void EffectBuffPlayer(NetworkPlayer player, Skill skill)
    {
        player.SetHaveEffect(skill.GetEffect().Clone());
        AnimationInstance(player, skill);
    }

    /// <summary>
    /// アニメーション再生
    /// </summary>
    void AnimationInstance(NetworkPlayer player, Skill skill)
    {
        GameObject efe = Instantiate(skill.GetEffectAnimation(), player.transform);
    }
}
