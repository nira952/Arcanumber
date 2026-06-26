using UnityEngine;

namespace nira.Demo
{
    public class DemoSkill : MonoBehaviour
    {
        /// <summary>
        /// サーバー側のみで実行される「ゲームのルール」に関する処理
        /// </summary>
        public void ExecuteLogicServerOnly(int myPlayerIndex)
        {
            Debug.Log($"[Server Logic] プレイヤー{myPlayerIndex}のスキル計算処理を実行しました。");
            // 例: 他のプレイヤーとの距離を計算してダメージを与えるなど
        }

        /// <summary>
        /// 全クライアント（自分含む）の画面で実行される「見た目」に関する処理
        /// </summary>
        public void PlayVisualEffect()
        {
            Debug.Log("[Client Visual] スキルのエフェクトを表示しました！");
            // 例: Instantiateでパーティクルを生成したり、音を鳴らす
        }
    }
}