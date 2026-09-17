using UnityEngine;
using R3;

/// <summary>
/// プレイヤーのアクションを管理するコントローラー
/// </summary>
public class PlayerActionController : MonoBehaviour
{
    [SerializeField] private AimCursor aim;

    public void Initialize()
    {
        if (aim != null) aim.Initialize();
    }

    public void LateUpdateAim()
    {
        if (aim != null) aim.AimUpdate();
    }

}