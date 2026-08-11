using UnityEngine;

public static class ActionHandler
{
    private const int ATTACK_ACTION_INDEX = 0;  //近接攻撃のインデックス

    /// <summary>
    /// 近接攻撃の攻撃用メソッド
    /// </summary>
    public static void ExecuteAttack(NetworkPlayer player)
    {
        //攻撃ができるか
        if (!player.IsActionReady(ATTACK_ACTION_INDEX)) return;
        player.StartActionCoolTime(ATTACK_ACTION_INDEX, player.GetAttackCoolTimeDuration());
        //スキル発動によって発動されるアルカナスキル
        player.GetArcana().ExecuteArcanaEffect(ASkillCategory.SkillEffect, player);
    }

    /// <summary>
    /// スキル変更用のメソッド
    /// </summary>
    public static void ExecuteSkillChange(NetworkPlayer player, int direction)
    {
        //スキル変更の方向に応じて次のスロット番号を計算
        int newSkillNo = CalculateNextSkillNo(player, direction);

        //新しい値をプレイヤーに反映
        player.SetSkillNo(newSkillNo);
        //エイム設定とUI更新
        UpdateSkillAimAndUI(player, newSkillNo);
    }


    /// <summary>
    /// スキル発動用のメソッド
    /// </summary>
    public static void ExecuteSkill(NetworkPlayer player)
    {
        //デバフがあったらスキル発動できない
        if (PlayerUtility.HaveEffect(player, EffectList.Silence, false)) return;

        //スキルのクールタイムチェック
        int currentNo = player.GetSkillNo();
        int actionIndex = PlayerUtility.GetCoolTimeIndex(currentNo);

        if (!player.IsActionReady(actionIndex))
        {
            Debug.Log($"枠 {currentNo} (配列位置: {actionIndex}) はクールタイム中だよ！");
            return;
        }

        //スキルの発動処理
        ExecuteSpecificSkill(player, currentNo, actionIndex);

        //スキル発動によって発動されるアルカナスキル
        player.GetArcana()?.ExecuteArcanaEffect(ASkillCategory.SkillEffect, player);
    }

    /// <summary>
    /// 次のスロット番号を計算するメソッド
    /// </summary>
    private static int CalculateNextSkillNo(NetworkPlayer player, int direction)
    {
        //現在のスキル番号に方向を加算して新しいスキル番号を計算
        int newSkillNo = player.GetSkillNo() + direction;

        //範囲制限のロジック
        if (newSkillNo < 0) newSkillNo = GameConfig.SKILL_HOPPER_MAX;
        else if (newSkillNo > GameConfig.SKILL_HOPPER_MAX) newSkillNo = 0;

        //アルカナがパッシブの時のスキップ処理
        bool isArcanaSlot = (newSkillNo == GameConfig.SKILL_HOPPER_MAX);
        bool isCommandArcana = player.GetArcana().GetASkillCategory() == ASkillCategory.Command;

        //アルカナスキルがコマンドスキルでない場合、アルカナスロットをスキップする
        if (isArcanaSlot && !isCommandArcana)
            newSkillNo = (direction > 0) ? 0 : GameConfig.SKILL_HOPPER_MAX - 1;

        return newSkillNo;
    }

    /// <summary>
    /// スキルのエイム設定とUI更新を行うメソッド
    /// </summary>
    private static void UpdateSkillAimAndUI(NetworkPlayer player, int newSkillNo)
    {
        //アルカナスキルが選択されていない場合、エイム設定を更新
        if (newSkillNo != GameConfig.SKILL_HOPPER_MAX)
        {
            var aimSelect = player.GetNoSkill()?.GetAimSelect();
            if (aimSelect != null)
                player.GetPlayerController().GetAimCursor().SelectAim((AimSelect)aimSelect);
        }
        //UIの更新
        PlayerUIManager.Instance.SkillFrameChange(player);
    }

    /// <summary>
    /// 指定されたスキルを実行するメソッド
    /// </summary>
    private static void ExecuteSpecificSkill(NetworkPlayer player, int currentNo, int actionIndex)
    {
        if (currentNo == GameConfig.SKILL_HOPPER_MAX)
        {
            //アルカナスキルの発動
            player.GetArcana().ExecuteArcanaEffect(ASkillCategory.Command, player);
            player.StartActionCoolTime(GameConfig.SKILL_ARCANA, player.GetArcana().GetCoolTime());
        }
        else
        {
            //通常スキルの発動
            Skill currentSkill = player.GetNoSkill();
            if (currentSkill != null)
            {
                SkillManager.Instance.RequestSkill(player);
                player.StartActionCoolTime(actionIndex, currentSkill.GetCoolTime());
            }
        }
    }
}
