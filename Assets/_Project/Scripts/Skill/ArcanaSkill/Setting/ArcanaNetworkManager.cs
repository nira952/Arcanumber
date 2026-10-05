using Unity.Netcode;
using UnityEngine;

public class ArcanaNetworkManager : NetworkBehaviour
{
    #region Singleton
    public static ArcanaNetworkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // オフラインモードかどうかを判定
        isLocalMode = PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsLocalMode;
    }
    #endregion


    bool isLocalMode = false;



    public void SetData(int index, Vector2 pos)
    {
        InstatiateEffectServerRpc(index, pos);
    }

    [ServerRpc]
    private void InstatiateEffectServerRpc(int index, Vector2 pos)
    {
        PlayerRoot player = PlayerUtility.GetPlayerByIndex(index);

        GameObject effectObj = Instantiate(player.GetArcana().GetEffectPrefab(), pos, Quaternion.identity);

        if (!isLocalMode && IsServer)
        {
            if (effectObj.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn();
            }
        }

        effectObj.transform.SetParent(player.transform);

        Animator animator = effectObj.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            //アニメーションの長さでオブジェクトを消す
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Destroy(effectObj, stateInfo.length);
        }
    }
}
