using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CurtainController : MonoBehaviour
{
    private Animator _animator;

    // アニメーションの状態名（AnimatorControllerのState名と一致させてください）
    private static readonly int OpenStateHash = Animator.StringToHash("Open");
    private static readonly int CloseStateHash = Animator.StringToHash("Close");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// カーテンを閉める（画面を隠す）
    /// </summary>
    public async UniTask CloseAsync(CancellationToken token = default)
    {
        _animator.Play(CloseStateHash, 0, 0f);

        // 💡 1フレーム待ってからアニメーション時間を取得（Play直後は前状態の長さが取れるため）
        await UniTask.Yield(PlayerLoopTiming.Update, token);

        // 再生が完了するまで待機
        await WaitAnimationCompleteAsync("Close", token);
    }

    /// <summary>
    /// カーテンを開ける（画面を見せる）
    /// </summary>
    public async UniTask OpenAsync(CancellationToken token = default)
    {
        _animator.Play(OpenStateHash, 0, 0f);
        await UniTask.Yield(PlayerLoopTiming.Update, token);

        await WaitAnimationCompleteAsync("Open", token);
    }

    // アニメーションが指定したStateかつ、NormalizedTimeが1（100%再生）になるまで待つ補助関数
    private async UniTask WaitAnimationCompleteAsync(string stateName, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

            // 指定したState名かつ、再生がループせずに最後まで到達したか判定
            if (stateInfo.IsName(stateName) && stateInfo.normalizedTime >= 1.0f)
            {
                break;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }
}