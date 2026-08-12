using System.Collections.Generic;
using UnityEngine;

public class ErodedArmorState : RefreshingState
{
    private const float ReductionPerStackPercent = -0.05f;

    public override States State => States.ErodedArmor;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    
    private readonly AttributeModifier _armorModifier = new AttributeModifier(0f, ModifierType.Percent);

    public override List<StatusEffect> Effects => new()
    {
        StatusEffect.Ability
    };

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = 3;

        _armorModifier.Source = this;

        currentStacksCount = 1;

        ApplyReduction();
    }

    public override bool Stack(float time)
    {
        duration = time;

        if (currentStacksCount >= MaxStacksCount)
        {
            ApplyReduction();
            return false;
        }

        currentStacksCount++;
        ApplyReduction();

        return true;
    }

    private void ApplyReduction()
    {
        if (characterState == null || characterState.Character == null) return;

        float newValue = currentStacksCount * ReductionPerStackPercent;

        var armorAttribute = characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical];

        if (armorAttribute != null)
        {
            if (!armorAttribute.Modifiers.Contains(_armorModifier))
                armorAttribute.AddModifier(_armorModifier);
            
            _armorModifier.Value = newValue;
        }
    }

    public override void ReduceStack()
    {
        currentStacksCount = 0;
        ExitState();
            
    }

    public override void ExitState()
    {
        RemoveReduction();
        currentStacksCount = 0;
        base.ExitState();
    }

    private void RemoveReduction()
    {
        if (characterState == null || characterState.Character == null) return;

        var armorAttribute = characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical];

        if (armorAttribute != null)
        {
            armorAttribute.RemoveModifier(_armorModifier);
        }

        _armorModifier.Value = 0f;
    }

    public override void UpdateState() { }

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        else
            Stack(duration);

        return this;
    }
}