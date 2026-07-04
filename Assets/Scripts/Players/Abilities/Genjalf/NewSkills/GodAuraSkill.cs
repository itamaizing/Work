using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GodAuraSkill : NetworkBehaviour
{
    private bool _isActive = false;
    public void OnAuraEnabled(GameObject character)
    {
        CmdAddState(character);
    }

    public void OnAuraDisabled(GameObject character)
    {
        CmdRemoveState(character);
    }
    
    
    [Command]
    private void CmdAddState(GameObject character)
    {
        if(character != null)
            character.GetComponent<Character>().CharacterState.AddState(States.GodAura,0,0,character.gameObject,nameof(GodAura));
    }

    [Command]
    private void CmdRemoveState(GameObject character)
    {
        if(character != null)
            character.GetComponent<Character>().CharacterState.RemoveState(States.GodAura);
    }
}

public class GodAura : AuraState
{
    public override States State => States.GodAura;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects { get; }
    public override float Distance => 8;
    public override float EffectRate { get; }
    public override LayerMask LayerMask => LayerMask.GetMask("Allies");

    public void AddTalentStack()
    {
        foreach (var character in _charactersInRadius)
        {
            if (character == null) continue;
            
            var buffState = character.CharacterState.GetState(States.GodAuraBuff) as GodAuraBuff;
            if(buffState != null)
                character.CharacterState.AddState(States.GodAuraBuff, 3f, 0,
                    characterState.Character.gameObject, nameof(GodAuraBuff));

        }
    }

    public override void EffectOnEnter(Character character)
    {
        if (characterState.Character == character) return;
        
        if (character.CharacterState.CheckForState(States.GodAuraBuff)) return;
    
        character.CharacterState.AddState(States.GodAuraBuff, -1, 0, 
            characterState.Character.gameObject, nameof(GodAuraBuff));
    }

    public override void EffectOnExit(Character character)
    {
        if (character.CharacterState.CheckForState(States.GodAuraBuff)) character.CharacterState.RemoveState(States.GodAuraBuff);
    }

    public override void EffectOnStay(List<Character> characters)
    {
        foreach (var character in characters)
        {
            if (character.CharacterState.CheckForState(States.GodAuraBuff)) continue;

            character.CharacterState.AddState(States.GodAuraBuff, -1, 0, 
                characterState.Character.gameObject, nameof(GodAuraBuff));
        }
    }

    public override void ExitState()
    {
        foreach (var character in _charactersInRadius)
        {
            if (character != null && character.CharacterState.CheckForState(States.GodAuraBuff))
                character.CharacterState.RemoveState(States.GodAuraBuff);
        }
        base.ExitState();
    }
}

public class GodAuraBuff : RefreshingState
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

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit,
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

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit,
        float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
        {
            _character = character.Character;
            MaxStacksCount = 4;
            _baseDuration = durationToExit;
            _stackTimer = durationToExit;
            currentStacksCount = 1;

            RemoveModifierFromAllSkills(_modifier);
            _modifier.Value = _modifierPerStack;
            ApplyModifierToAllSkills(_modifier);
        }
        else if (currentStacksCount < MaxStacksCount)
        {
            if (durationToExit > 0)
            {
                currentStacksCount++;
                _baseDuration = durationToExit;
                _stackTimer = durationToExit;
                UpdateModifier(currentStacksCount);
            }
        }

        return this;
    }

    private void UpdateModifier(int stacks)
    {
        RemoveModifierFromAllSkills(_modifier);
        _modifier.Value = _modifierPerStack * stacks;
        ApplyModifierToAllSkills(_modifier);
    }

    public override void GloabalUpdate()
    {
        UpdateState();
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
            
            _stackTimer = _baseDuration;
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
        foreach (var skill in _character.Abilities.Abilities)
            skill.Attributes.Attributes[SkillAttributeName.Cooldown].AddModifier(modifier);
    }

    private void RemoveModifierFromAllSkills(AttributeModifier modifier)
    {
        if (_character == null) return;
        foreach (var skill in _character.Abilities.Abilities)
            skill.Attributes.Attributes[SkillAttributeName.Cooldown].RemoveModifier(modifier);
    }
}
