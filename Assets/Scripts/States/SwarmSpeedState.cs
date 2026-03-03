using System.Collections.Generic;
using UnityEngine;

public class SwarmSpeedState : AuraState
{
    private const float SpeedMultiplier = 1.3f;
    private const float AuraRadius = 10f;

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
            skill.Buff.CastSpeed.IncreasePercentage(SpeedMultiplier);
        }

        _buffedCharacters.Add(character);
    }

    private void RemoveSpeed(Character character)
    {
        if (!_buffedCharacters.Contains(character)) return;

        foreach (var skill in character.Abilities.Abilities)
        {
            skill.Buff.CastSpeed.ReductionPercentage(SpeedMultiplier);
        }

        _buffedCharacters.Remove(character);
    }

    public override void ExitState()
    {
        foreach (var character in _buffedCharacters)
        {
            foreach (var skill in character.Abilities.Abilities)
            {
                skill.Buff.CastSpeed.ReductionPercentage(SpeedMultiplier);
            }
        }

        _buffedCharacters.Clear();
        base.ExitState();
    }

    public override void UpdateState() { }
}