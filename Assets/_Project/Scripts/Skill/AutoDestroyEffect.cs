using UnityEngine;

/// <summary>
/// エフェクトの再生が終わったら自動的に削除するスクリプト
/// </summary>
public class AutoDestroyEffect : MonoBehaviour
{
    private Animator _animator;

    void Start()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        //アニメーションがある場合、終了したら削除
        if (_animator != null)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            //アニメーションが1回再生され、遷移中でないなら削除
            if (stateInfo.normalizedTime >= 1.0f && !_animator.IsInTransition(0))
                Destroy(gameObject);
        }
    }
}