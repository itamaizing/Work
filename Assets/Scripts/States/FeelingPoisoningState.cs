using System.Collections.Generic;
using UnityEngine;

public class FeelingPoisoningStateStacking : StateStackingRefreshing
{
    private const int MaxStacks = 6;
    private const float RegenPerStack = 10f;
    
    public override float StackLingerTime => 0.1f;

    public override States State => States.FeelingPoisoning;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => new()
    {
        StatusEffect.Strengthening
    };

    public override void Apply(
        CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        SetMaxStacks(MaxStacks);
        
        CurrentStacksCount = 1;
        ApplyRegenBonus();
    }

    public override bool Stack(float time)
    {
        RemainingDuration = time;

        if (CurrentStacksCount >= MaxStacksCount)
            return true;

        ApplyRegenBonus();
        CurrentStacksCount++;

        return true;
    }

    public override void ReduceStack()
    {
        base.ReduceStack();

        if (CurrentStacksCount <= 0)
            return;

        characterState.Character.Resource.Attr_RegenValue
            .RemoveBySource(this, all: false);
    }

    protected override void OnExit()
    {
        characterState.Character.Resource.Attr_RegenValue
            .RemoveBySource(this, all: true);

        CurrentStacksCount = 0;
    }

    private void ApplyRegenBonus()
    {
        characterState.Character.Resource.Attr_RegenValue.AddModifier(new AttributeModifier(RegenPerStack, ModifierType.Flat, source: this));
    }
}