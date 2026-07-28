using UnityEngine;
using R3;

public class PlayerActionController : MonoBehaviour
{
    [SerializeField] private AimCursor aim;

    private readonly Subject<Unit> jumpSubject = new();
    public Observable<Unit> OnJumpAsObservable => jumpSubject;

    private readonly Subject<Unit> attackSubject = new();
    public Observable<Unit> OnAttackAsObservable => attackSubject;

    private readonly Subject<int> skillSelectSubject = new();
    public Observable<int> OnSkillSelectAsObservable => skillSelectSubject;

    private readonly Subject<Unit> skillUseSubject = new();
    public Observable<Unit> OnSkillUseAsObservable => skillUseSubject;

    public void Initialize()
    {
        if (aim != null) aim.Initialize();
    }

    public void LateUpdateAim()
    {
        if (aim != null) aim.AimUpdate();
    }

    // --- コントローラー（オフライン/オンライン）から呼ばれるローカル実行用メソッド ---
    public void ExecuteJumpLocal() => jumpSubject.OnNext(Unit.Default);
    public void ExecuteAttackLocal() => attackSubject.OnNext(Unit.Default);
    public void ExecuteSkillSelectLocal(int dir) => skillSelectSubject.OnNext(dir);
    public void ExecuteSkillUseLocal() => skillUseSubject.OnNext(Unit.Default);

    private void OnDestroy()
    {
        jumpSubject.Dispose();
        attackSubject.Dispose();
        skillSelectSubject.Dispose();
        skillUseSubject.Dispose();
    }
}