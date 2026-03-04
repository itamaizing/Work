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
    private float _basePercentage = 0.1f;
    private float _currentPercentage = 0f;
    private Character _character;

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
        ApplyBonus(_basePercentage);
    }

    public void RefreshBonus(int talentStacks)
    {
        RemoveBonus(_currentPercentage);
        float newPercentage = _basePercentage + talentStacks * 0.1f;
        ApplyBonus(newPercentage);
    }

    private void ApplyBonus(float percentage)
    {
        _currentPercentage = percentage;
        foreach (var skill in _character.Abilities.Abilities)
            skill.Buff.Cooldown.IncreasePercentage(1f - percentage);
    }

    private void RemoveBonus(float percentage)
    {
        if (percentage <= 0f) return;
        foreach (var skill in _character.Abilities.Abilities)
            skill.Buff.Cooldown.IncreasePercentage(1f / (1f - percentage));
    }

    public override void ExitState()
    {
        RemoveBonus(_currentPercentage);
        _currentPercentage = 0f;
        currentStacksCount = 0;
        _character.CharacterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (time < 0)
        {
            currentStacksCount = 1;
            return false;
        }

        if (CurrentStacksCount >= MaxStacksCount)
            return false;

        currentStacksCount++;
        return true;
    }
    public override void UpdateState() { }
}
