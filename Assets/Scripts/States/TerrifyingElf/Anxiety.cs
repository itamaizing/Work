using System.Collections.Generic;
using UnityEngine;

public class Anxiety : AbstractCharacterState
{
    private float spellSpeedReduction = 0.1f;
    private float manaCostIncrease = 0.1f;
    private const int maxStacks = 3;

    private List<StatusEffect> _effects = new List<StatusEffect> { StatusEffect.Ability, StatusEffect.Strengthening };
    public override States State => States.Anxiety;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        abilities = character.Character.Abilities;
        health = character.Character.Health;
        MaxStacksCount = maxStacks;

        ApplyEffects();
        Debug.Log($"Anxiety state applied: {currentStacksCount}/{MaxStacksCount} stacks, duration {duration}s");
    }

    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
        RemoveEffects();
        Debug.Log($"Anxiety state removed: {currentStacksCount}/{MaxStacksCount} stacks");
    }

    public override bool Stack(float newDuration)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
        }
        duration = Mathf.Max(duration, newDuration);
        ApplyEffects();
        return true;
    }

    private void ApplyEffects()
    {
        if (abilities != null)
        {
            foreach (var skill in abilities.Abilities)
            {
                skill.CastDeley *= 1f + (spellSpeedReduction * currentStacksCount);

                foreach (var cost in skill.SkillEnergyCosts)
                {
                    cost.ModifyResourceCost(1f + (manaCostIncrease * currentStacksCount));
                }
            }
        }
    }

    private void RemoveEffects()
    {
        if (abilities != null)
        {
            foreach (var skill in abilities.Abilities)
            {
                skill.CastDeley /= 1f + (spellSpeedReduction * currentStacksCount);

                foreach (var cost in skill.SkillEnergyCosts)
                {
                    cost.ModifyResourceCost(1f / (1f + (manaCostIncrease * currentStacksCount)));
                }
            }
        }
    }
}
