using System.Collections.Generic;
using UnityEngine;

public class CorrodedArmorState : AbstractCharacterState
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
        MaxStacksCount = 5;
        currentStacksCount = 1;
    }

    public override void EnterState(CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.personWhoMadeBuff = personWhoMadeBuff;

        _durationRemaining = durationToExit;

        ApplyReduction();
    }

    public override void UpdateState()
    {
        _durationRemaining -= Time.deltaTime;

        if (_durationRemaining <= 0)
            ExitState();
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

        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveState(this);
    }
}