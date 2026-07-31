using R3;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerRoot))]
public class DebugPlayerPreview : MonoBehaviour
{
    private PlayerRoot root;

    [SerializeField] private int previewPlayerIndex;
    [SerializeField] private int previewCurrentHealth;

    [SerializeField] private bool previewIsDown;
    [SerializeField] private bool previewIsMove;
    [SerializeField] private bool previewIsJump;
    [SerializeField] private bool previewIsChangeMove;
    [SerializeField] private bool previewIsNormalAttack;
    [SerializeField] private List<EffectAbility> previewActiveEffects = new();

#if UNITY_EDITOR
    private void Awake()
    {
        root = GetComponent<PlayerRoot>();
    }


    private void Start()
    {
        root.PlayerIndex.Subscribe(v => previewPlayerIndex = v).AddTo(this);
        root.CurrentHealth.Subscribe(v => previewCurrentHealth = v).AddTo(this);
        root.IsDown.Subscribe(v => previewIsDown = v).AddTo(this);
        root.IsMove.Subscribe(v => previewIsMove = v).AddTo(this);
        root.IsJump.Subscribe(v => previewIsJump = v).AddTo(this);
        root.IsChangeMove.Subscribe(v => previewIsChangeMove = v).AddTo(this);
        root.IsNormalAttack.Subscribe(v => previewIsNormalAttack = v).AddTo(this);
        root.ActiveEffects.Subscribe(v => previewActiveEffects = v).AddTo(this);
    }

#endif
}
