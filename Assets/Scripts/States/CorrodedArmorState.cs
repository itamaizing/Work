using System.Collections.Generic;
using UnityEngine;

public class CorrodedArmorState : StateStackingRefreshing
{
    private const float ReductionPerStack = 2f;
    private float _durationRemaining;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.CorrodedArmor;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;
    public override float RemainingDuration => _durationRemaining;

    public CorrodedArmorState()
    {
        SetMaxStacks(5);
        CurrentStacksCount = 1;
    }

    public override void Apply(CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.sourceCaster = personWhoMadeBuff;

        _durationRemaining = durationToExit;

        ApplyReduction();
    }

    public override void UpdateState()
    {
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
        }

        _durationRemaining = time;

        ApplyReduction();
        return true;
    }

    private void ApplyReduction()
    {
        if (characterState == null) return;

        var attribute = characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical];

        attribute.RemoveBySource(this);
        float totalReduction = -(CurrentStacksCount * ReductionPerStack);
        attribute.AddModifier(new AttributeModifier(totalReduction, ModifierType.Flat, this));
    }

    public override void ExitState()
    {
        var attribute = characterState?.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical];
        attribute?.RemoveBySource(this);

        CurrentStacksCount = 1;

        characterState.RemoveState(this);
    }
}