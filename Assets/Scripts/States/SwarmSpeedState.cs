using System.Collections.Generic;
using UnityEngine;

public class SwarmSpeedState : RefreshingState
{
    private const float BaseBonus = 0.30f;
    private const float PerUnitBonus = 0.05f;

    private AttributeModifier _speedModifier;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.AbilitySpeed };

    private List<Skill> _skills = new();
    
    public override States State => States.SwarmSpeed;
    public override StateType Type => StateType.Aura;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;
    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        float occupiedCapacity = damageToExit;
        
        float multiplier = 1f + BaseBonus + (occupiedCapacity * PerUnitBonus);
  
        foreach (var ability in character.Character.Abilities.Abilities)
        {
            if (ability.Info.AbilityForm == AbilityForm.Physical || ability.Info.AbilityForm == AbilityForm.Both)
            {
                _speedModifier = new AttributeModifier(multiplier, ModifierType.Multiplier, this);
                ability.Attributes[SkillAttributeName.CastSpeed].AddModifier(_speedModifier);
                _skills.Add(ability);
            }
        }
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

        foreach (var ability in _skills)
        {
            ability.Attributes[SkillAttributeName.CastSpeed].RemoveModifier(_speedModifier);
        }

        _skills.Clear();
        
        _speedModifier = null;
    }
}