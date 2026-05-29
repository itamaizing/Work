using System.Collections.Generic;
using UnityEngine;

public class FeelingPoisoningState : RefreshingState
{
    private const int MaxStacks = 6;
    private const float RegenPercentPerStack = 0.1f;

    private Resource resource;
    private float _baseRegen;

    public override States State => States.FeelingPoisoning;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => new()
    {
        StatusEffect.Strengthening
    };

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = MaxStacks;
        ApplyRegenBonus();
    }

    public override void UpdateState()
    {

    }

    public override bool Stack(float time)
    {
        duration = time;

        if (currentStacksCount < MaxStacksCount)
        {
            ApplyRegenBonus();
            currentStacksCount++;
        }

        return true;
    }

    public override void ExitState()
    {
        characterState.Character.Resource.Attr_RegenValue.RemoveBySource(this, all: true);
        base.ExitState();
    }

    public override void ReduceStack()
    {
        currentStacksCount--;

        if (currentStacksCount <= 0)
        {
            ExitState();
            return;
        }
        characterState.Character.Resource.Attr_RegenValue.RemoveBySource(this, all: false);
    }

    private void ApplyRegenBonus()
    {
        characterState.Character.Resource.Attr_RegenValue.AddModifier(
            new AttributeModifier(RegenPercentPerStack, ModifierType.Percent, source: this));
    }

}