using UnityEngine;

namespace nira.Demo
{
    /// <summary>
    /// プレイヤーの行動（入力の反映）を処理するインターフェース
    /// </summary>
    public interface IPlayerActionHandler
    {
        /// <summary>
        /// 現在、入力を受け付けてよい状態か（権限チェックなど）
        /// </summary>
        bool CanProcessInput { get; }

        /// <summary>
        /// ダメージ処理を要請する
        /// </summary>
        void RequestTakeDamage(int damage);

        void RequestActivateSkill();
    }
}