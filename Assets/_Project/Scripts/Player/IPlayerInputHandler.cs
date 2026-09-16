using R3;

public interface IPlayerInputHandler
{
    Observable<float> OnMoveAsObservable { get; }

    Observable<Unit> OnJumpAsObservable { get; }

    Observable<Unit> OnAttackAsObservable { get; }

    Observable<int> OnSkillSelectAsObservable { get; }

    Observable<Unit> OnSkillUseAsObservable { get; }
}