using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GodAuraSkill : MonoBehaviour
{

}

public class GodAura : AuraState
{
    public override States State => States.GodAura;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects { get; }
    public override float Distance => 8;
    public override float EffectRate { get; }
    public override LayerMask LayerMask => LayerMask.GetMask("Allies");

    public int TalentStacks { get; private set; } = 0;

    public void AddTalentStack()
    {
        if (TalentStacks >= 3) return;
        TalentStacks++;

        foreach (var character in _charactersInRadius)
        {
            if (character == null) continue;
            
            character.CharacterState.AddState(States.GodAuraBuff, 3f, 0,
                characterState.Character.gameObject, nameof(GodAuraBuff));

            var buffState = character.CharacterState.GetState(States.GodAuraBuff) as GodAuraBuff;
            buffState?.RefreshBonus(TalentStacks);
        }
    }

    public void RemoveTalentStack()
    {
        if (TalentStacks <= 0) return;
        TalentStacks--;

        foreach (var character in _charactersInRadius)
        {
            if (character == null) continue;
            
            character.CharacterState.StateIcons.RemoveIconCount();
            
            var buffState = character.CharacterState.GetState(States.GodAuraBuff) as GodAuraBuff;
            buffState?.RefreshBonus(TalentStacks);
        }
    }
    
    public void ResetToBaseAura()
    {
        TalentStacks = 0;

        foreach (var character in _charactersInRadius)
        {
            if (character == null) continue;
            
            character.CharacterState.RemoveState(States.GodAuraBuff);
            character.CharacterState.AddState(States.GodAuraBuff, -1, 0,
                characterState.Character.gameObject, nameof(GodAuraBuff));

            var buffState = character.CharacterState.GetState(States.GodAuraBuff) as GodAuraBuff;
            buffState?.RefreshBonus(0);
        }
    }

    public override void EffectOnEnter(Character character)
    {
        if (characterState.Character == character) return;
        
        character.CharacterState.AddState(States.GodAuraBuff, -1, 0, characterState.Character.gameObject, nameof(GodAuraBuff));
    }

    public override void EffectOnExit(Character character)
    {
        if (character.CharacterState.CheckForState(States.GodAuraBuff)) character.CharacterState.RemoveState(States.GodAuraBuff);
    }

    public override void EffectOnStay(List<Character> characters) { }

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

public class GodAuraBuff : AbstractCharacterState
{
    private List<StatusEffect> _effects = new List<StatusEffect>();
    private Character _character;

    private AttributeModifier _baseModifier = new AttributeModifier(-0.1f, ModifierType.Percent);

    private AttributeModifier _stackModifier = new AttributeModifier(0f, ModifierType.Percent);

    private int _talentStacks = 0;
    private float _stackTimer = 0f;
    private float _stackDuration = 3f;

    public override States State => States.GodAuraBuff;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        _character = character.Character;
        MaxStacksCount = 3;
        currentStacksCount = 1;

        ApplyModifierToAllSkills(_baseModifier);
    }

    public void RefreshBonus(int talentStacks)
    {
        RemoveModifierFromAllSkills(_stackModifier);

        _talentStacks = talentStacks;
        _stackModifier.Value = -talentStacks * 0.1f;

        if (_talentStacks > 0)
        {
            ApplyModifierToAllSkills(_stackModifier);
            _stackTimer = _stackDuration;
            currentStacksCount = 1 + _talentStacks;
        }
        else
        {
            _stackModifier.Value = 0f;
            currentStacksCount = 1;
            _stackTimer = 0f;

            RemoveModifierFromAllSkills(_baseModifier);
            ApplyModifierToAllSkills(_baseModifier);
        }
    }

    public override void UpdateState()
    {
        if (_talentStacks <= 0) return;

        _stackTimer -= Time.deltaTime;

        if (_stackTimer <= 0f)
        {
            RemoveModifierFromAllSkills(_stackModifier);
            _talentStacks = 0;
            _stackModifier.Value = 0f;
            currentStacksCount = 1;
        }
    }

    public override void ExitState()
    {
        RemoveModifierFromAllSkills(_baseModifier);
        RemoveModifierFromAllSkills(_stackModifier);
        _talentStacks = 0;
        currentStacksCount = 0;
        _character.CharacterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return true;
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
