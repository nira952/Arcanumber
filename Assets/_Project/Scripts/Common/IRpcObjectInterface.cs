using R3;

public interface IRpcObjectInterface
{

    public void RpcInitialize(int playerIndex);

    public Observable<Unit> OnDestroyed { get; }
}