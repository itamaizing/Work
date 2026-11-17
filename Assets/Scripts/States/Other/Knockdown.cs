using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knockdown : StateStackingRefreshing
{
    private float _baseDuration;

    public override States State => States.Knockdown;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => new List<StatusEffect> { StatusEffect.Strengthening };

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        CurrentStacksCount = 1;

        float resistance = characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical].GetValue();
        float chanceToApply = 100f - resistance;

        if (Random.Range(0f, 100f) > chanceToApply)
        {
            Debug.Log("Knockdown was resisted due to high physical resistance");
            ExitState();
            return;
        }

        RemainingDuration = durationToExit;
        _baseDuration = durationToExit;
        SetMaxStacks(3);

        ApplyDebuff();
    }

    protected override void OnExit()
    {
        RemoveDebuff();
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            RemainingDuration = _baseDuration;
            ApplyDebuff();
            return true;
        }

        RemainingDuration = _baseDuration;
        return false;
    }

    private void ApplyDebuff()
    {
        var abilities = characterState.GetComponentInChildren<SkillManager>();
        foreach (var ability in abilities.Abilities)
        {
            float reduction = 1f + (0.01f * CurrentStacksCount);
            ability.Buff.Damage.ReductionPercentage(reduction);
        }
    }

    private void RemoveDebuff()
    {
        var abilities = characterState.GetComponentInChildren<SkillManager>();
        foreach (var ability in abilities.Abilities)
        {
            float reduction = 1f + (0.01f * CurrentStacksCount);
            ability.Buff.Damage.IncreasePercentage(reduction);
        }
    }
}