using UnityEngine;

public interface IPlayerActionHandler
{
    bool CanProcessInput { get; }

    void RequestJump();
    void RequestAttack();
    void RequestSkillSelect(int direction);
    void RequestSkillUse();
    void RequestTakeDamage(int damage);
}