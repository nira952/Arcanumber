using R3;
using UnityEngine;
using UnityEngine.LowLevel;

/// <summary>
/// プレイヤー入力を購読し、現在のIPlayerInputMediatorへ変換して転送するスクリプト
/// </summary>
public class PlayerInputBinder : MonoBehaviour
{
    [SerializeField] private PlayerInputController inputController;

    // 現在アクティブなMediator（Network, Offlineなど）を保持
    private IPlayerInputMediator currentMediator;


    private void Awake()
    {
        if (inputController == null)
        {
            inputController = GetComponent<PlayerInputController>();

            Debug.LogError("PlayerInputControllerがアタッチされていません。");
        }
    }


    /// <summary>
    /// 入力の購読を登録し、発生した入力を指定した IPlayerInputMediator に転送する。
    /// </summary>
    public void Initialize(IPlayerInputMediator mediator)
    {
        currentMediator = mediator;

        // ここで一括購読し、インターフェースのメソッドに変換して流す
        inputController.OnAttackAsObservable
            .Where(_ => CanProcessInput())
            .Subscribe(_ => currentMediator?.OnAttackTriggered())
            .AddTo(this);

        inputController.OnJumpAsObservable
            .Where(_ => CanProcessInput())
            .Subscribe(_ => currentMediator?.OnJumpTriggered())
            .AddTo(this);

        inputController.OnMoveAsObservable
            .Where(_ => CanProcessInput())
            .Subscribe(direction => currentMediator?.OnMoveTriggered(direction))
            .AddTo(this);

        inputController.OnSkillSelectAsObservable
            .Where(_ => CanProcessInput())
            .Subscribe(skillIndex => currentMediator?.OnSkillSelectTriggered(skillIndex))
            .AddTo(this);

        inputController.OnSkillUseAsObservable
            .Where(_ => CanProcessInput())
            .Subscribe(_ => currentMediator?.OnSkillUseTriggered())
            .AddTo(this);

    }

    private bool CanProcessInput()
    {
        // 1. ネットワーク経由の場合、自分が所有者（IsOwner）でないなら入力を受け付けない
        // ※ currentMediator が NetworkController の場合などの所有権チェック
        if (currentMediator is PlayerNetworkController netController)
        {
            if (!netController.IsSpawned || !netController.IsOwner) return false;
        }

        // 2. PlayerRoot 側のゲームルール判定（ダウン中・ポーズ中など）をチェック
        return true;
    }
}