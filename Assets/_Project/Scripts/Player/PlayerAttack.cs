using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

// プレイヤーの通常攻撃を管理するクラス
public class PlayerAttack : NetworkBehaviour
{
   [SerializeField]  private PlayerRoot root;

    private float attackPower = 3.0f; // 攻撃力の倍率
    private float attackUpPower = 5.5f; // パワーアップ時の攻撃力の倍率
    private float attackWindupTime = 0.5f; // 攻撃の前振り時間

    private bool isLocalMode = false; // ローカルモードかどうかのフラグ

    [SerializeField] private NormalSlash slashObject;

    private void Start()
    {
        if (slashObject == null)
        {
            Debug.LogError("Slash object is not assigned in the inspector.");
            return;
        }

        isLocalMode = PlayerDataManager.Instance.IsLocalMode;
    }

    public void Initialized(PlayerRoot root,bool isPowerUp)
    {
        this.root = root;

        int playerIndex = root.PlayerIndex.Value;

        float damage = isPowerUp ? attackUpPower : attackPower;

        // 攻撃オブジェクトを初期化する
        slashObject.Initialize(playerIndex, damage, -1);

    }



    public void NormalAttackActive(Vector3 direction)
    {

        if (isLocalMode)
        {
            // ローカルモードでは通常攻撃のオブジェクトを0.5秒アクティブにする
            ExecuteAttack(direction).Forget();
        }
        else if (IsOwner)
        {
            if (IsServer)
            {
                // 通常攻撃のオブジェクトを0.5秒アクティブにする
                AttackServerRpc(direction);
            }
        }
    }

    [ServerRpc]
    private void AttackServerRpc(Vector3 direction)
    {
        ExecuteAttack(direction).Forget();

    }
    [ClientRpc]
    private void PlayAttackEffectClientRpc()
    {
        slashObject.ActiveAniation("RedSlash");
    }

    private async UniTaskVoid ExecuteAttack(Vector3 direction)
    {
        // 向きを変更する
        transform.localScale = direction;

        root.CanMove.Value = false; // 攻撃中は移動を禁止する

        await UniTask.Delay((int)(attackWindupTime * 1000)); // 前振り時間を待機

        // 攻撃のシーケンスを非同期で実行する
        slashObject.ActiveAttack(); // 攻撃アニメーションを再生

        PlayAttackEffectClientRpc();

        await UniTask.Delay(300); // 0.5秒待機

        slashObject.EndAttack(); // 攻撃終了処理

        root.CanMove.Value = true; // 攻撃終了後に移動を許可する
    }

    /// <summary>
    /// 通常攻撃を振る前にヒット済み対象リストをリセットする
    /// </summary>
    public void ResetList()
    {
        slashObject.ResetList();
    }
}
