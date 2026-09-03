using System;
using System.Collections.Generic;
using UnityEngine;

public class HardenedFlesh : AbstractCharacterState
{
    private List<StatusEffect> _effects = new() { StatusEffect.Destruction };

    public override States State => States.HardenedFlesh;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    private const float BuffPercentPerStack = 0.05f;
    private int _currentStacks = 0;
    private const int _maxStacks = 5;

    private AttributeModifier _resistanceModifier;
    
    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        abilities = character.Character.Abilities;
        base.personWhoMadeBuff = personWhoMadeBuff;

        duration = durationToExit;
        _currentStacks = 1;

        ApplyOrUpdateModifier();
    }


    public override void ExitState()
    {
        RemoveModifier();
        characterState.RemoveState(this);
    }


    public override bool Stack(float time)
    {
        duration = time;

        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            ApplyOrUpdateModifier();
        }

        return false;
    }

    public override void UpdateState()
    {
        if (duration <= 0)
        {
            ExitState();
            return;
        }
    }
    
    private void ApplyOrUpdateModifier()
    {
        if (characterState?.Character == null) return;

        var resistanceAttr = characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical];
        float totalBonus = _currentStacks * BuffPercentPerStack;

        if (_resistanceModifier == null)
        {
            _resistanceModifier = new AttributeModifier(totalBonus, ModifierType.Percent, this);
            resistanceAttr.AddModifier(_resistanceModifier);
        }
        else
        {
            _resistanceModifier.Value = totalBonus;
        }
    }
    
    private void RemoveModifier()
    {
        if (characterState?.Character == null || _resistanceModifier == null) return;

        var resistanceAttr = characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical];
        resistanceAttr.RemoveModifier(_resistanceModifier);
        _resistanceModifier = null;
    }
}
