using System.Collections.Generic;
using UnityEngine;

public class LightningEvadeState : RefreshingState
{
    private float _evadePerStack = 0.10f; 

    public override States State => States.LightningEvade;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    private readonly AttributeModifier _evadePhysicalModifier = new AttributeModifier(0f, ModifierType.Percent);
    private readonly AttributeModifier _evadeMagicalModifier = new AttributeModifier(0f, ModifierType.Percent);

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

        currentStacksCount = 1;

        ApplyEvade();
    }

    public override bool Stack(float time)
    {
        duration = time;

        if (currentStacksCount >= MaxStacksCount)
            return false;

        currentStacksCount++;
        ApplyEvade();

        return true;
    }

    private void ApplyEvade()
    {
        float newValue = currentStacksCount * _evadePerStack;

        var physical = characterState.Character.AttributeSystem[CharacterAttributeName.EvasionPhysical];
        var magical = characterState.Character.AttributeSystem[CharacterAttributeName.EvasionMagical];

        if (!physical.Modifiers.Contains(_evadePhysicalModifier))
            physical.AddModifier(_evadePhysicalModifier);

        if (!magical.Modifiers.Contains(_evadeMagicalModifier))
            magical.AddModifier(_evadeMagicalModifier);

        _evadePhysicalModifier.Value = newValue;
        _evadeMagicalModifier.Value = newValue;
    }

    public override void ReduceStack()
    {
        currentStacksCount--;
        ExitState();
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        RemoveEvade();
        base.ExitState();
    }

    private void RemoveEvade()
    {
        var physical = characterState.Character.AttributeSystem[CharacterAttributeName.EvasionPhysical];
        var magical = characterState.Character.AttributeSystem[CharacterAttributeName.EvasionMagical];

        physical.RemoveModifier(_evadePhysicalModifier);
        magical.RemoveModifier(_evadeMagicalModifier);

        _evadePhysicalModifier.Value = 0f;
        _evadeMagicalModifier.Value = 0f;
    }

    public override void UpdateState() { }
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        else
            Stack(duration);

        return this;
    }
}