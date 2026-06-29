using UnityEngine;

public static class ActionHandler
{
    public static void ExecuteAttack(NetworkPlayer player)
    {
        //攻撃ができるか
        if (!player.IsActionReady(0)) return;
        player.StartActionCoolTime(0, player.GetAttackCoolTimeDuration());
        //スキル発動によって発動されるアルカナスキル
        player.GetArcana().ExecuteArcanaEffect(ASkillCategory.SkillEffect, player);
    }

    public static void ExecuteSkillChange(NetworkPlayer player, int direction)
    {
        //現在の skillNo を取得
        int newSkillNo = player.GetSkillNo() + direction;

        //範囲制限のロジック
        if (newSkillNo < 0) newSkillNo = GameConfig.SKILL_HOPPER_MAX;
        else if (newSkillNo > GameConfig.SKILL_HOPPER_MAX) newSkillNo = 0;

        //アルカナがパッシブの時のスキップ処理
        if (newSkillNo == GameConfig.SKILL_HOPPER_MAX &&
            player.GetArcana().GetASkillCategory() != ASkillCategory.Command)
        {
            newSkillNo = (direction > 0) ? 0 : GameConfig.SKILL_HOPPER_MAX - 1;
        }

        //新しい値をプレイヤーに反映
        player.SetSkillNo(newSkillNo);
        //エイム設定
        if(newSkillNo != GameConfig.SKILL_HOPPER_MAX)
            player.GetPlayerController().GetAimCursor()
                .SelectAim(player.GetNoSkill().GetAimSelect());

        BattleUIManager.Instance.SkillFrameChange(player);
    }

    public static void ExecuteSkill(NetworkPlayer player)
    {
        //スキルが発動できるか
        if (PlayerUtility.HaveEffect(player, EffectList.Silence, false))
        {
            return;
        }

        //スキルホッパーの更新
        int currentNo = player.GetSkillNo();
        int actionIndex = PlayerUtility.GetCoolTimeIndex(currentNo);
        if (!player.IsActionReady(actionIndex))
        {
            Debug.Log($"枠 {currentNo} (配列位置: {actionIndex}) はクールタイム中だよ！");
            return;
        }

        if (currentNo == GameConfig.SKILL_HOPPER_MAX)
        {
            player.GetArcana().ExecuteArcanaEffect(ASkillCategory.Command, player);

            //Arcanaデータが持っているクールタイムを設定
            player.StartActionCoolTime(5, player.GetArcana().GetCoolTime());
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
        //スキル発動によって発動されるアルカナスキル
        player.GetArcana().ExecuteArcanaEffect(ASkillCategory.SkillEffect, player);
    }
}
