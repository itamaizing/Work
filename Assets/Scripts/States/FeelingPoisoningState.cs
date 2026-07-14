using System.Collections.Generic;
using UnityEngine;

public class FeelingPoisoningState : RefreshingState
{
    private const int MaxStacks = 6;
    private const float RegenPercentPerStack = 0.1f;

    private Energy _energy;
    private float _baseRegen;

    public override States State => States.FeelingPoisoning;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => new()
    {
        StatusEffect.Strengthening
    };

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = MaxStacks;

        _energy = character.Character.GetComponent<Energy>();

        if (_energy == null)
        {
            Debug.LogError("FeelingPoisoningState: Energy not found");
            return;
        }

        if (currentStacksCount == 0)
        {
            _baseRegen = _energy.RegenerationValue;
        }

        ApplyRegenBonus();
    }

    public override void OnUpdateState()
    {

    }

    public override bool Stack(float time)
    {
        duration = time;

        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
        }

        ApplyRegenBonus();

        return true;
    }

    protected override void OnExitState()
    {
        RemoveRegenBonus();
    }

    protected override void OnReduceStack(int count = 1)
    {
        currentStacksCount-=count;

        if (currentStacksCount <= 0)
        {
            ExitState();
            return;
        }

        ApplyRegenBonus();
    }

    private void ApplyRegenBonus()
    {
        if (_energy == null) return;

        float multiplier = 1f + (currentStacksCount * RegenPercentPerStack);
        _energy.RegenerationValue = _baseRegen * multiplier;
    }

    private void RemoveRegenBonus()
    {
        if (_energy == null) return;

        _energy.RegenerationValue = _baseRegen;
    }
}