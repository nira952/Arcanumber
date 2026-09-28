using Unity.Netcode;
using R3; 

public static class NetworkVariableExtensions
{
    // NetworkVariable の変化を R3.Observable<T> に変換する拡張メソッド
    public static Observable<T> AsObservable<T>(this NetworkVariable<T> networkVariable) where T : unmanaged
    {
        return Observable.FromEvent<NetworkVariable<T>.OnValueChangedDelegate, T>(
            h => (oldV, newV) => h(newV),
            h => networkVariable.OnValueChanged += h,
            h => networkVariable.OnValueChanged -= h
        ).Prepend(networkVariable.Value); // 現在の値も最初に流す
    }
}