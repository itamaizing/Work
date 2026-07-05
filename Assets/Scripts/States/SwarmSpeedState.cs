using System.Collections.Generic;
using UnityEngine;

public class SwarmSpeedState : AuraState
{
    private float _currentCountWarm;
    private float _appliedMultiplier;

    private const float AuraRadius = 10f;
    private const float BaseBonus = 0.30f;
    private const float PerStackBonus = 0.05f;

    private readonly HashSet<Character> _buffedCharacters = new();

    public override States State => States.SwarmSpeed;
    public override StateType Type => StateType.Aura;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new() { StatusEffect.AbilitySpeed };

    public override float Distance => AuraRadius;
    public override float EffectRate => 0.2f;
    public override LayerMask LayerMask => LayerMask.GetMask("Allies");

    public override void EffectOnEnter(Character character)
    {
        if (character == null) return;
        if (character == _self) return;
        if (_buffedCharacters.Contains(character)) return;

        ApplySpeed(character);
    }

    public override void EffectOnExit(Character character)
    {
        if (character == null) return;
        RemoveSpeed(character);
    }

    public override void EffectOnStay(List<Character> characters)
    {

    }

    private void ApplySpeed(Character character)
    {
        foreach (var skill in character.Abilities.Abilities)
        {
            skill.Buff.CastSpeed.IncreasePercentage(_appliedMultiplier);
        }

        _buffedCharacters.Add(character);
    }

    private void RemoveSpeed(Character character)
    {
        if (!_buffedCharacters.Contains(character)) return;

        foreach (var skill in character.Abilities.Abilities)
        {
            skill.Buff.CastSpeed.ReductionPercentage(_appliedMultiplier);
        }

        _buffedCharacters.Remove(character);
    }

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _currentCountWarm = damageToExit;
        _appliedMultiplier = 1f + BaseBonus + (_currentCountWarm * PerStackBonus);

        var existing = character.GetState(State);
        if (existing != null)
        {
            existing.RemainingDuration = durationToExit;
            return existing;
        }

        return base.TryApply(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
    }

    protected override void ExitState()
    {
        foreach (var character in _buffedCharacters)
        {
            foreach (var skill in character.Abilities.Abilities)
            {
                skill.Buff.CastSpeed.ReductionPercentage(_appliedMultiplier);
            }
        }

        _buffedCharacters.Clear();
    }

    public override void UpdateState() { }
}