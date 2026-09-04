using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class MagicWaterAura : AuraStateHandler
{
    [SerializeField] private float _buffDuration = -1f;

    protected override void OnTargetEnter(Character target)
    {
        CmdApplyStateToTarget(target.gameObject, States.MagicWater, _buffDuration, Schools.Water, _owner.gameObject,
            nameof(MagicWater),0);
    }

    protected override void OnTargetExit(Character target)
    {
        CmdRemoveStateFromTarget(target.gameObject, States.MagicWater);
    }

    protected override void OnAuraDisabled()
    {
        RemoveEffectsFromAllTargets();
    }
}

public class MagicWater : AbstractCharacterState
{
    private List<StatusEffect> _effects = new List<StatusEffect>();

    private const float ManaMaxPercent = 0.10f;
    private const float ManaRegenPercent = 0.003f;
    private const float TickInterval = 1f;

    private readonly AttributeModifier _maxManaModifier =
        new AttributeModifier(ManaMaxPercent, ModifierType.Percent);

    private Coroutine _regenCoroutine;
    private Resource _manaResource;

    public override States State => States.MagicWater;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState characterState, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        duration = durationToExit;
        this.characterState = characterState;

        _maxManaModifier.Source = this;

        if (characterState.Character != null && 
            characterState.Character.Resources.TryGetValue(ResourceType.Mana, out var mana))
        {
            _manaResource = mana;
        }

        ApplyBuffs();
        StartRegenRoutine();
    }

    private void ApplyBuffs()
    {
        if (_manaResource != null)
        {
            _manaResource.AddModifier(ResourceAttributeName.MaxValue, _maxManaModifier);
        }
    }

    private void RemoveBuffs()
    {
        if (_manaResource != null)
        {
            _manaResource.RemoveModifierBySource(ResourceAttributeName.MaxValue, this);
        }
    }

    private void StartRegenRoutine()
    {
        if (characterState?.Character == null || _manaResource == null) return;
        
        if (characterState.Character.isServer || characterState.Character.isServerOnly)
        {
            _regenCoroutine = characterState.StartCoroutine(RegenRoutine());
        }
    }

    private void StopRegenRoutine()
    {
        if (_regenCoroutine != null && characterState != null)
        {
            characterState.StopCoroutine(_regenCoroutine);
            _regenCoroutine = null;
        }
    }

    private IEnumerator RegenRoutine()
    {
        var waitForInterval = new WaitForSeconds(TickInterval);

        while (true)
        {
            yield return waitForInterval;

            if (_manaResource != null)
            {
                float regenAmount = _manaResource.MaxValue * ManaRegenPercent;
                if (regenAmount > 0)
                {
                    _manaResource.Add(regenAmount);
                }
            }
        }
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        StopRegenRoutine();
        RemoveBuffs();
        base.ExitState();
    }
    public override bool Stack(float time) => false;

    public override void UpdateState()
    {
    }
}
