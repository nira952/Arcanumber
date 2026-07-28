using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class CurtainController : MonoBehaviour
{
    [Header("Loading Screen")]
    [SerializeField] private TextMeshProUGUI loadingMessageText;

    [SerializeField] private Image curtainImage;

    private Animator _animator;

    // アニメーションの状態名（AnimatorControllerのState名と一致させてください）
    private static readonly int OpenStateHash = Animator.StringToHash("Open");

    private static readonly int FullOpenStateHash = Animator.StringToHash("FullOpen");
    private static readonly int CloseStateHash = Animator.StringToHash("Close");

    private void Awake()
    {
        _animator = GetComponent<Animator>();

#if UNITY_EDITOR
        // 実行時にSceneビュー上でのみ非表示にする
        if (Application.isPlaying)
        {
            SceneVisibilityManager.instance.Hide(gameObject, true);
        }
#endif
    
    }


    /// <summary>
    /// カーテンを閉める（画面を隠す）
    /// </summary>
    public async UniTask CloseAsync(CancellationToken token = default)
    {
        // 1. Closeを再生（Animator側で自動遷移はさせず、Closeで止まるようにしておく）
        _animator.Play(CloseStateHash, 0, 0f);

        // カーテンが閉まったら、UIの操作をブロックするためにRaycastTargetを有効化
        curtainImage.raycastTarget = true; 

        await UniTask.Yield(PlayerLoopTiming.Update, token);

        // 2. Closeがしっかり終了するまで待つ（状態が勝手に変わらないので確実に検知可能）
        await WaitAnimationCompleteAsync("Close", token);

        // 3. 待ち終わったら、スクリプトから明示的にIdleに戻す
        _animator.Play("CloseIdle", 0, 0f);
    }
    /// <summary>
    /// カーテンを開ける（画面を見せる）
    /// </summary>
    public async UniTask OpenAsync(CancellationToken token = default)
    {
        _animator.Play(OpenStateHash, 0, 0f);
        await UniTask.Yield(PlayerLoopTiming.Update, token);

        await WaitAnimationCompleteAsync("Open", token);

        // カーテンが開いたら、UIの操作ブロックを解除
        curtainImage.raycastTarget = false;

        loadingMessageText.text = string.Empty;
    }

    public async UniTask FullOpenAsync(CancellationToken token = default)
    {
        _animator.Play(FullOpenStateHash, 0, 0f);
        await UniTask.Yield(PlayerLoopTiming.Update, token);

        await WaitAnimationCompleteAsync("FullOpen", token);

        // カーテンが開いたら、UIの操作ブロックを解除
        curtainImage.raycastTarget = false;

        loadingMessageText.text = string.Empty;

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

    public void UpdateLoadingMessage(string message)
    {
        if (loadingMessageText != null) loadingMessageText.text = message;
    }

    public void HideLoaingMessage()
    {
        if (loadingMessageText != null) loadingMessageText.text = string.Empty;
    }

}