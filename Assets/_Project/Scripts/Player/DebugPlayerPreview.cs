using ObservableCollections;
using R3;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerRoot))]
public class DebugPlayerPreview : MonoBehaviour
{
    private PlayerRoot root;

    [SerializeField] private string previewCurrentSkill;

    [SerializeField] private string previewCurrentArcana;
    [SerializeField] private int previewPlayerIndex;
    [SerializeField] private float previewCurrentHealth;

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
        previewCurrentArcana = !string.IsNullOrEmpty(root.GetArcana()?.GetArcanaName) ? root.GetArcana().GetArcanaName : "•s–¾";

        root.PlayerIndex.Subscribe(v => previewPlayerIndex = v).AddTo(this);
        root.CurrentHealth.Subscribe(v => previewCurrentHealth = v).AddTo(this);
        root.IsDown.Subscribe(v => previewIsDown = v).AddTo(this);
        root.ActiveEffects.ObserveAdd().Subscribe(v => previewActiveEffects.Add(v.Value)).AddTo(this);
    }

#endif
}
