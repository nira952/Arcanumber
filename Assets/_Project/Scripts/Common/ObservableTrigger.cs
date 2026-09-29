using UnityEngine;
using R3;

public class ObservableTrigger : MonoBehaviour
{
    private readonly Subject<Unit> onTriggered = new();

    public Observable<Unit> OnTriggered =>
        onTriggered.AsObservable();

    public void Trigger()
    {
        onTriggered.OnNext(Unit.Default);
    }

    private void OnDestroy()
    {
        onTriggered.OnCompleted();
        onTriggered.Dispose();
    }
}