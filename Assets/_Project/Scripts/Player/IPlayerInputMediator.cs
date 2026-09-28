public interface IPlayerInputMediator
{
    void OnMoveTriggered(float direction);

    void OnJumpTriggered();

    void OnAttackTriggered();

    void OnSkillSelectTriggered(int skillIndex);

    void OnSkillUseTriggered();
}