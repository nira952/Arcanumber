using UnityEngine;

public interface IPlayerActionHandler
{
    // 入力を処理してよいか（オンラインなら IsOwner かつ生存、オフラインなら生存かどうかの判定に使う）
    bool CanProcessInput { get; }

    // アクションの実行要求
    void RequestJump();
    void RequestAttack();
    void RequestSkillSelect(int direction);
    void RequestSkillUse();
}