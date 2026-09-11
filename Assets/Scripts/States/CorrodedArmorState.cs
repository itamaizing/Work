using System.Collections.Generic;
using UnityEngine;

public class CorrodedArmorState : RefreshingStateStacking
{
    private const float ReductionPerStack = 2f;
    private float _durationRemaining;
    private float _appliedReduction = 0f;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.CorrodedArmor;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;
    public override float RemainingDuration => _durationRemaining;

    public CorrodedArmorState()
    {
        SetMaxStacks(5);
        currentStacksCount = 1;
    }

    public override void Apply(CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.sourceCaster = personWhoMadeBuff;

        _durationRemaining = durationToExit;

        ApplyReduction();
    }

    public override void UpdateState()
    {

    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
        }

        _durationRemaining = time;

        ApplyReduction();
        return true;
    }

    private void ApplyReduction()
    {
        if (health == null) return;

        float totalReduction = currentStacksCount * ReductionPerStack;

        health.DefPhysDamage -= _appliedReduction;

        _appliedReduction = totalReduction;
        health.DefPhysDamage -= _appliedReduction;
    }

    public override void ExitState()
    {
        if (health != null)
        {
            health.DefPhysDamage += _appliedReduction;
        }
        
        currentStacksCount = 1;
        _appliedReduction = 0f;

        
        characterState.RemoveState(this);
    }
}