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

public class GodAuraBuff : RefreshingStateStacking
{
    private List<StatusEffect> _effects = new List<StatusEffect>();
    private Character _character;

    private AttributeModifier _modifier = new AttributeModifier(-0.1f, ModifierType.Percent);
    private const float _modifierPerStack = -0.1f;

    private float _baseDuration;
    private float _stackTimer;

    public override States State => States.GodAuraBuff;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount == 0)
        {
            return true;
        }
        
        _baseDuration = time;
        _stackTimer = _baseDuration;

        UpdateModifier(currentStacksCount + 1);

        return true;
    }

    

    private void UpdateModifier(int stacks)
    {
        RemoveModifierFromAllSkills(_modifier);
        _modifier.Value = _modifierPerStack * stacks;
        ApplyModifierToAllSkills(_modifier);
    }

    public override void UpdateState()
    {
        if (_stackTimer <= -1) return;

        _stackTimer -= Time.deltaTime;

        if (_stackTimer <= 0)
        {
            currentStacksCount--;

            if (currentStacksCount <= 0)
            {
                ExitState();
                return;
            }
            
            _stackTimer = currentStacksCount == 1 ? -1f : _baseDuration;
            if (_stackTimer == -1f)
            {
    
            }
            UpdateModifier(currentStacksCount);
        }
    }

    public override void ExitState()
    {
        RemoveModifierFromAllSkills(_modifier);

        currentStacksCount = 0;
        _baseDuration = 0;
        _stackTimer = 0;
        _modifier.Value = _modifierPerStack;
        _character = null;
    
        if (characterState != null && characterState.CheckForState(States.GodAuraBuff))
            characterState.RemoveState(this);
    
        characterState = null;
        
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
