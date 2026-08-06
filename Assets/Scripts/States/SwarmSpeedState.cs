using System.Collections.Generic;
using UnityEngine;

public class SwarmSpeedState : RefreshingState
{
    private const float BaseBonus = 0.30f;
    private const float PerUnitBonus = 0.05f;

    private AttributeModifier _speedModifier;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.AbilitySpeed };
    public override States State => States.SwarmSpeed;
    public override StateType Type => StateType.Aura;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;
    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        float occupiedCapacity = damageToExit;
        
        float multiplier = 1f + BaseBonus + (occupiedCapacity * PerUnitBonus);
        _speedModifier = new AttributeModifier(multiplier, ModifierType.Multiplier, this);
        characterState.Character.AttributeSystem[CharacterAttributeName.CastSpeedPhysical].AddModifier(_speedModifier);
    }

    public override void UpdateState() { }

    public override void ExitState()
    {
        currentStacksCount = 0;
        RemoveSpeedModifier();
        base.ExitState();
    }

    private void RemoveSpeedModifier()
    {
        if (_speedModifier == null) return;

        characterState.Character.AttributeSystem[CharacterAttributeName.CastSpeedPhysical].RemoveBySource(this);

        _speedModifier = null;
    }
}