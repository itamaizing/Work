using System.Collections.Generic;
using UnityEngine;

public class Anxiety : StateStacking
{
    private float spellSpeedReduction = 0.1f;
    private float manaCostIncrease = 0.1f;
    private const int maxStacks = 3;

    private List<StatusEffect> _effects = new List<StatusEffect> { StatusEffect.Ability, StatusEffect.Strengthening };
    public override States State => States.Anxiety;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        abilities = character.Character.Abilities;
        health = character.Character.Health;
        SetMaxStacks(maxStacks);

        ApplyEffects();
        Debug.Log($"Anxiety state applied: {CurrentStacksCount}/{MaxStacksCount} stacks, duration {RemainingDuration}s");
    }

    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
        RemoveEffects();
        Debug.Log($"Anxiety state removed: {CurrentStacksCount}/{MaxStacksCount} stacks");
    }

    public override bool Stack(float newDuration)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
        }
        RemainingDuration = Mathf.Max(RemainingDuration, newDuration);
        ApplyEffects();
        return true;
    }

    private void ApplyEffects()
    {
        //Теперь есть опция вешать дебаффы на самого героя
        if (abilities != null)
        {
            foreach (var skill in abilities.Abilities)
            {
                skill.CastDeley *= 1f + (spellSpeedReduction * CurrentStacksCount);

                if (CurrentStacksCount < MaxStacksCount)
                    skill.Attributes[SkillAttributeName.ResourceCost].AddModifier(new AttributeModifier(manaCostIncrease, ModifierType.Flat, source: this));
                //foreach (var cost in skill.SkillEnergyCosts)
                //{
                //    cost.ModifyResourceCost(1f + (manaCostIncrease * CurrentStacksCount));
                //}
                Debug.Log(skill.Attributes[SkillAttributeName.ResourceCost].GetValue());
            }
        }
        
    }

    private void RemoveEffects()
    {
        if (abilities != null)
        {
            foreach (var skill in abilities.Abilities)
            {
                skill.CastDeley /= 1f + (spellSpeedReduction * CurrentStacksCount);

                skill.Attributes[SkillAttributeName.ResourceCost].RemoveBySource(this, all: true);
                //foreach (var cost in skill.SkillEnergyCosts)
                //{
                //    cost.ModifyResourceCost(1f / (1f + (manaCostIncrease * CurrentStacksCount)));
                //}
            }
        }
    }
}
