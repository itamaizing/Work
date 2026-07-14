using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DivineEnhancementState : AbstractCharacterState, IDamageGivenModifier
{
    private float _duration;
    private float _manaCostModifierValue = 2f;
    private Character _character;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.DivineEnhancement;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => new() { StatusEffect.Ability };

    private List<Skill> _costSkills = new();

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _character = character.Character;
        _duration = durationToExit;
        ModifyManaCost();
    }

    public override void OnUpdateState()
    {

    }

    protected override void OnExitState()
    {
        ResetManaCost();
    }

   /* public override bool Stack(float time)
    {
        _duration = time;
        return true;
    }*/

    private void ModifyManaCost()
    {
        foreach (var skill in _character.Abilities.Abilities)
        {
            if (skill.Damage > 0)
            {
                _costSkills.Add(skill);
                skill.Buff.ManaCost.IncreasePercentage(_manaCostModifierValue);
            }
        }
    }

    private void ResetManaCost()
    {
        foreach (var skill in _costSkills)
        {
            skill.Buff.ManaCost.ReductionPercentage(_manaCostModifierValue);
        }
        
        _costSkills.Clear();
    }

    public float ModifyOutgoingDamage(Damage damage)
    {
        return damage.Value * 2f;
    }
}
