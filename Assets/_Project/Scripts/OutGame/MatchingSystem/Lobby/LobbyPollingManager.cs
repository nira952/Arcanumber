using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Netcode;

/// <summary>
/// 定期的なポーリングを管理するクラス
/// </summary>
public class LobbyPollingManager
{
    private const int PollingIntervalMs = 1000;

    public async UniTask StartHostPollingAsync(Func<bool> isHostCondition, Action onUpdate, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (NetworkManager.Singleton != null && isHostCondition())
            {
                onUpdate?.Invoke();
            }
            await UniTask.Delay(PollingIntervalMs, cancellationToken: token);
        }
    }

    public async UniTask RunPollingLoopAsync(Func<bool> condition, Action updateAction, CancellationToken token)
    {
        while (!token.IsCancellationRequested && condition())
        {
            updateAction?.Invoke();
            await UniTask.Delay(PollingIntervalMs, cancellationToken: token);
        }
    }
}