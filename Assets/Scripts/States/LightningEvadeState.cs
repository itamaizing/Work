using System.Collections.Generic;
using UnityEngine;

public class LightningEvadeState : StackableState
{
    private float _evadePerStack = 10f;

    public override States State => States.LightningEvade;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    private readonly AttributeModifier _evadePhysicalModifier = new AttributeModifier(0, ModifierType.Percent);
    private readonly AttributeModifier _evadeMagicalModifier = new AttributeModifier(0, ModifierType.Percent);

    public override List<StatusEffect> Effects => new()
    {
        StatusEffect.Evade,
        StatusEffect.Strengthening
    };

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = 4;

        _evadePhysicalModifier.Source = this;
        _evadeMagicalModifier.Source = this;

        ApplyEvade();
    }

    public override bool Stack(float time)
    {
        duration = time;

        if (currentStacksCount >= MaxStacksCount)
            return false;

        ApplyEvade();
        currentStacksCount++;

        return true;
    }

    private void ApplyEvade()
    {
        float newValue = _evadePhysicalModifier.Value + _evadePerStack;

        _evadePhysicalModifier.Value = newValue;
        _evadeMagicalModifier.Value = newValue;

        var physical = characterState.Character.AttributeSystem[CharacterAttributeName.EvasionPhysical];
        var magical = characterState.Character.AttributeSystem[CharacterAttributeName.EvasionMagical];

        if (!physical.Modifiers.Contains(_evadePhysicalModifier))
            physical.AddModifier(_evadePhysicalModifier);

        if (!magical.Modifiers.Contains(_evadeMagicalModifier))
            magical.AddModifier(_evadeMagicalModifier);
    }

    public override void ExitState()
    {
        RemoveEvade();
        base.ExitState();
    }

    public override void ReduceStack()
    {
        _evadePhysicalModifier.Value -= _evadePerStack;
        _evadeMagicalModifier.Value -= _evadePerStack;

        currentStacksCount--;

        if (currentStacksCount <= 0) ExitState();
    }

    private void RemoveEvade()
    {
        characterState.Character.AttributeSystem[CharacterAttributeName.EvasionPhysical].RemoveBySource(this);
        characterState.Character.AttributeSystem[CharacterAttributeName.EvasionMagical].RemoveBySource(this);

        _evadePhysicalModifier.Value = 0;
        _evadeMagicalModifier.Value = 0;
    }

    public override void UpdateState() { }
}