using Unity.Netcode;
using UnityEngine;

public class ParticleDestroy : NetworkBehaviour
{
    private ParticleSystem targetParticle;
    private float timer = 0f;
    private float effectDuration = 0f;

    private void Awake()
    {
        //ParticleSystemを自動取得
        targetParticle = GetComponentInChildren<ParticleSystem>();

        if (targetParticle != null)
        {
            //パーティクルの再生時間を自動で取得する
            var main = targetParticle.main;
            effectDuration = main.duration;

            //ループしている場合は、強制的にループを切るか、安全のため最大秒数を担保する
            if (main.loop)
                Debug.LogWarning("パーティクルがLoop設定になっています。自動でDespawnさせるためLoopを解除するか、長さを調整してください。");
        }
    }

    private void Update()
    {
        //サーバー側でのみ処理
        if (!IsServer) return;
        if (targetParticle == null) return;

        //経過時間を進める
        timer += Time.deltaTime;

        //再生時間が経過したらDespawnする
        if (timer >= effectDuration)
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
        }
    }
}
