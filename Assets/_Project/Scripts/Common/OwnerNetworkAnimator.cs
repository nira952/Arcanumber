using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class OwnerNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // クライアント（Owner）主導の同期にする
    }
}