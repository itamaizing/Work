using System.Collections.Generic;
using UnityEngine;

public class DefenceReductionState : StateBasic
{
    private float _healthBuffActiveTime = 2f;
    private float _healthBoostPercentage = 0.25f;

    private List<StatusEffect> _effects = new();
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.DefenseReduction;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _healthBuffActiveTime = durationToExit;
        _healthBoostPercentage = damageToExit;
        ApplyBuff();
    }

    public override void UpdateState()
    {
        _healthBuffActiveTime -= Time.deltaTime;

        if (_healthBuffActiveTime <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        RemoveBuff();
        characterState.RemoveState(this);
    }

    private void ApplyBuff()
    {
        characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical]
            .AddModifier(new AttributeModifier(_healthBoostPercentage, ModifierType.Multiplier, this));
    }

    private void RemoveBuff()
    {
        characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical].RemoveBySource(this);
    }
}