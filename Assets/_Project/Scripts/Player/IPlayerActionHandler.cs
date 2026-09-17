using UnityEngine;

public interface IPlayerActionHandler
{
    bool CanProcessInput { get; }

    void RequestAttack();
    void RequestSkillUse();
}