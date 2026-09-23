using System.Collections.Generic;
using UnityEngine;

public class IrradiationState : StateStackingRefreshing
{
    private float _baseDuration;
    private float _durationIncrease = 1;
    private const float _magicDefenseReduction = 3;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Irradiation;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering Irradiation State");
        characterState = character;

        _baseDuration = durationToExit;
        RemainingDuration = _baseDuration;

        SetMaxStacks(3);

        characterState.OnStateAdded += OnNewStateAdded;

        ExtendExistingNegativeMagic();
        ApplyMagicDefenseReduction();
    }

    public override void UpdateState()
    {
        RemainingDuration -= Time.deltaTime;
        if (RemainingDuration <= 0) ExitState();
    }

    public override void ExitState()
    {
        RestoreMagicDefense();
        characterState.RemoveState(this);
        characterState.OnStateAdded -= OnNewStateAdded;
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            RemainingDuration = _baseDuration;
            ApplyMagicDefenseReduction();

            Debug.Log($"Stacking Irradiation. Current stacks: {CurrentStacksCount}, New duration: {RemainingDuration}s");
            return true;
        }
        else
        {
            RemainingDuration = _baseDuration;
            Debug.Log($"Max stacks reached. Refreshing Irradiation duration: {RemainingDuration}s");
            return false;
        }
    }

    private void ApplyMagicDefenseReduction()
    {
        characterState.Character.AttributeSystem[CharacterAttributeName.ResistanceMagical]
            .AddModifier(new AttributeModifier(-_magicDefenseReduction, ModifierType.Flat, this));
    }

    private void RestoreMagicDefense()
    {
        characterState.Character.AttributeSystem[CharacterAttributeName.ResistanceMagical].RemoveBySource(this);
    }

    private void OnNewStateAdded(StateBasic newState)
    {
        if (newState != this && newState.Type == StateType.Magic && newState.BaffDebaff == BaffDebaff.Debaff) ExtendState(newState);
    }

    private void ExtendExistingNegativeMagic()
    {
        foreach (var state in characterState.CurrentStates)
            if (state != this && state.Type == StateType.Magic && state.BaffDebaff == BaffDebaff.Debaff) ExtendState(state);
    }

    private void ExtendState(StateBasic state)
    {
        //state.duration += _durationIncrease;
        //state.RemainingDuration += _durationIncrease;
    }
}