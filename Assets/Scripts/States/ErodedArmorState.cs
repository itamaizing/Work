using System.Collections.Generic;
using UnityEngine;

public class ErodedArmorState : RefreshingState
{
    private const float ReductionPerStackPercent = 0.05f;

    private AttributeModifier _armorModifier;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.ErodedArmor;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public ErodedArmorState()
    {
        MaxStacksCount = 3;
    }

    public override void EnterState(CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.personWhoMadeBuff = personWhoMadeBuff;

        currentStacksCount = 1;
        
        _armorModifier = new AttributeModifier(ReductionPerStackPercent, ModifierType.Percent, this);

        ApplyReduction();
    }

    public override void UpdateState()
    {
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
        }

        duration = time;

        ApplyReduction();
        return true;
    }

    private void ApplyReduction()
    {
        if (characterState == null || characterState.Character == null) return;
        
        _armorModifier.Value = -(currentStacksCount * ReductionPerStackPercent);
        
        var armorAttribute = characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical];
        
        if (armorAttribute != null && !armorAttribute.Modifiers.Contains(_armorModifier))
        {
            armorAttribute.AddModifier(_armorModifier);
        }
    }

    public override void ExitState()
    {
        if (characterState != null && characterState.Character != null)
        {
            var armorAttribute = characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical];

            if (armorAttribute != null && armorAttribute.Modifiers.Contains(_armorModifier))
            {
                armorAttribute.RemoveModifier(_armorModifier);
            }
        }

        currentStacksCount = 0;

        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveState(this);
    }
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
        {
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        }
        else
            Stack(duration);

        return this;
    }
}