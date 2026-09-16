using R3;
using UnityEngine;

public class AutoInputController : MonoBehaviour,IPlayerInputHandler
{
    private readonly Subject<float> moveSubject = new();
    public Observable<float> OnMoveAsObservable => moveSubject;

    private readonly Subject<Unit> jumpSubject = new();
    public Observable<Unit> OnJumpAsObservable => jumpSubject;

    private readonly Subject<Unit> attackSubject = new();
    public Observable<Unit> OnAttackAsObservable => attackSubject;

    private readonly Subject<int> skillSelectSubject = new();
    public Observable<int> OnSkillSelectAsObservable => skillSelectSubject;

    private readonly Subject<Unit> skillUseSubject = new();
    public Observable<Unit> OnSkillUseAsObservable => skillUseSubject;

}
