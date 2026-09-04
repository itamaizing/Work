using System;
using System.Collections.Generic;
using UnityEngine;

public class HardenedFlesh : RefreshingState
{
    private List<StatusEffect> _effects = new() { StatusEffect.Destruction };

    public override States State => States.HardenedFlesh;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    private const float BuffPerStack = 5f;
    private const int _maxStacks = 5;

    private AttributeModifier _resistanceModifier;
    
    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        abilities = character.Character.Abilities;
        base.personWhoMadeBuff = personWhoMadeBuff;
        
        currentStacksCount = 1;
        MaxStacksCount = _maxStacks;
        duration = durationToExit;

        ApplyOrUpdateModifier();
    }
    

    public override void ExitState()
    {
        base.ExitState();
        RemoveModifier();
        characterState.StateIcons.RemoveItemByState(State);
        currentStacksCount = 0;
    }


    public override bool Stack(float time)
    {
        if (currentStacksCount < _maxStacks)
        {
            currentStacksCount++;
            ApplyOrUpdateModifier();
        }

        return true;
    }

    public override void UpdateState()
    {
    }
    
    private void ApplyOrUpdateModifier()
    {
        if (characterState?.Character == null) return;

        var resistanceAttr = characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical];
        float totalBonus = currentStacksCount * BuffPerStack;

        if (_resistanceModifier == null)
        {
            _resistanceModifier = new AttributeModifier(totalBonus, ModifierType.Flat, this);
            resistanceAttr.AddModifier(_resistanceModifier);
        }
        else
        {
            _resistanceModifier.Value = totalBonus;
        }
    }
    
    public override void ReduceStack()
    {
        ExitState();
    }
    
    private void RemoveModifier()
    {
        if (characterState?.Character == null || _resistanceModifier == null) return;

        var resistanceAttr = characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical];
        resistanceAttr.RemoveModifier(_resistanceModifier);
        _resistanceModifier = null;
    }
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        if (currentStacksCount == 0)
        {
            BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        }
        else
        {
            duration = durationToExit;
            Stack(durationToExit);
        }

        return this;
    }
}
