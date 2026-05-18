using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DivineEnhancementState : AbstractCharacterState, IDamageGivenModifier
{
    private float _duration;
    private float _manaCostModifierValue = 2f;
    private Character _character;
    private AttributeModifier _modifier = new AttributeModifier(1, ModifierType.Multiplier);

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.DivineEnhancement;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => new() { StatusEffect.Ability };

    private List<Skill> _costSkills = new();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _character = character.Character;
        _duration = durationToExit;
        ModifyManaCost();
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0) ExitState();
    }

    public override void ExitState()
    {
        ResetManaCost();
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        _duration = time;
        return true;
    }

    private void ModifyManaCost()
    {
        characterState.Character.AttributeSystem[CharacterAttributeName.ResourceCost].AddModifier(_modifier);
    }

    private void ResetManaCost()
    {
        characterState.Character.AttributeSystem[CharacterAttributeName.ResourceCost].RemoveModifier(_modifier);
    }

    public float ModifyOutgoingDamage(Damage damage)
    {
        return damage.Value * 2f;
    }
}
