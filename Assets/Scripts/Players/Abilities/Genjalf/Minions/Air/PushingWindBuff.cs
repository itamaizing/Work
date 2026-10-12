using System.Collections.Generic;
using UnityEngine;

public class PushingWindBuff : StateBasic
{
    private const float BuffSpeedBonus = 0.3f;
    private const float AuraSpeedBonus = 0.1f;

    private AttributeModifier _modifier = new(0, ModifierType.Multiplier);
    private bool _isModifierApplied;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State { get; }
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects { get; } = new();

    private bool isAuraState => State == States.PushingWindAura;

    public PushingWindBuff(States stateType)
    {
        State = stateType;
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        EnsureModifier();
    }
    
    public override void Reapply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        EnsureModifier();
    }

    private void EnsureModifier()
    {
        if (_isModifierApplied) return;

        _modifier.Value = 1 + (isAuraState ? AuraSpeedBonus : BuffSpeedBonus);
        _modifier.Type = ModifierType.Multiplier;
        characterState.Character.Move.AddModifier(_modifier);
        _isModifierApplied = true;
    }

    protected override void OnExit()
    {
        if (_isModifierApplied)
        {
            characterState.Character.Move.RemoveModifier(_modifier);
            _isModifierApplied = false;
        }
    }
}