using Cysharp.Threading.Tasks;
using UnityEngine;

// プレイヤーの通常攻撃を管理するクラス
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private NormalSlash slashObject;

    private void Start()
    {
        if (slashObject == null)
        {
            Debug.LogError("Slash object is not assigned in the inspector.");
            return;
        }

        

        slashObject.gameObject.SetActive(false); // 初期状態では攻撃オブジェクトを非アクティブにする   
    }

    public void Initialized(PlayerRoot root)
    {
        int playerIndex = root.PlayerIndex.Value;

        slashObject.Initialize(playerIndex, 1, -1);
    }


    public void Flip(float moveInput)
    {
        // 入力がほぼ 0 の場合は直前の向きを維持
        if (Mathf.Abs(moveInput) <= 0.01f) return;

        Vector3 currentScale = transform.localScale;

        // 右移動 (moveInput > 0) なら Scale.x を正、左移動 (moveInput < 0) なら負にする
        // ※ 元のスプライトが「左向き」基準で作られている場合は、不等号を逆にしてください
        if (moveInput > 0f)
        {
            currentScale.x = -Mathf.Abs(currentScale.x);
        }
        else if (moveInput < 0f)
        {
            currentScale.x = Mathf.Abs(currentScale.x);
        }

        transform.localScale = currentScale;
    }



    public void NormalAttackActive()
    {
        Debug.Log("NormalAttackActive called.");

        // 通常攻撃のオブジェクトを0.5秒アクティブにする
        StartAttackSequenceAsync().Forget();

    }

    private async UniTaskVoid StartAttackSequenceAsync()
    {
        // 攻撃のシーケンスを非同期で実行する
        slashObject.gameObject.SetActive(true); // 攻撃オブジェクトをアクティブにする
        await UniTask.Delay(500); // 0.5秒待機
        slashObject.gameObject.SetActive(false); // 攻撃オブジェクトを非アクティブにする
    }

    public void ResetList()
    {
        slashObject.ResetList();
    }
}
