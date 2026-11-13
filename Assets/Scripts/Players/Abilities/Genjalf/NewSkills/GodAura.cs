using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GodAura : AuraStateHandler
{
    private float _buffDuration = -1f;

    protected override void OnTargetEnter(Character target)
    {
        CmdApplyStateToTarget(target.gameObject, States.GodAuraBuff, _buffDuration,Schools.None, 
            _owner.gameObject, nameof(GodAura),0);
    }

    protected override void OnTargetExit(Character target)
    {
        Debug.Log(target.gameObject);
        CmdRemoveStateFromTarget(target.gameObject, States.GodAuraBuff);
    }

    protected override void OnAuraDisabled()
    {
        RemoveEffectsFromAllTargets();
    }
    
    public void AddTalentStack()
    {
        foreach (var character in _currentTargets)
        {
            if (character == null) continue;
            
            var buffState = character.CharacterState.GetState(States.GodAuraBuff) as GodAuraBuff;
            if(buffState != null)
                character.CharacterState.CmdAddState(States.GodAuraBuff, 3f, 0,
                    character.gameObject, nameof(GodAuraBuff));
        }
    }
}

public class GodAuraBuff : StateStackingRefreshing
{
    private List<StatusEffect> _effects = new List<StatusEffect>();
    private Character _character;

    private AttributeModifier _modifier = new AttributeModifier(-0.1f, ModifierType.Percent);
    private const float _modifierPerStack = -0.1f;

    public override States State => States.GodAuraBuff;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override string TooltipDescription => $"Уменьшает перезарядку способностей на {CurrentStacksCount * 10}%";

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        SetMaxStacks(1);
        CurrentStacksCount = 1;
        RemainingDuration = durationToExit;
    }

    public override void OnStackChange()
    {
        Debug.Log(characterState);
        UpdateModifier(CurrentStacksCount);
    }


    private void UpdateModifier(int stacks)
    {
        RemoveModifierFromAllSkills(_modifier);
        _modifier.Value = _modifierPerStack * stacks;
        ApplyModifierToAllSkills(_modifier);
    }

    protected override void OnExit()
    {
        RemoveModifierFromAllSkills(_modifier);

        CurrentStacksCount = 0;
        _modifier.Value = _modifierPerStack;
    }

    private void ApplyModifierToAllSkills(AttributeModifier modifier)
    {
        if (_character == null) return;
        _character.AttributeSystem.Attributes[CharacterAttributeName.CooldownReduction].AddModifier(modifier);
        /*foreach (var skill in _character.Abilities.Abilities)
            skill.Attributes.Attributes[SkillAttributeName.Cooldown].AddModifier(modifier);*/
        
    }

    private void RemoveModifierFromAllSkills(AttributeModifier modifier)
    {
        if (_character == null) return;
        _character.AttributeSystem.Attributes[CharacterAttributeName.CooldownReduction].RemoveModifier(modifier);
        /*foreach (var skill in _character.Abilities.Abilities)
            skill.Attributes.Attributes[SkillAttributeName.Cooldown].RemoveModifier(modifier);*/
    }
}
