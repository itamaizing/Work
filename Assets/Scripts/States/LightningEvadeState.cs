using System.Collections.Generic;
using UnityEngine;

public class LightningEvadeState : RefreshingState
{
    private float _evadePerStack = 10f;
    private float _totalEvade = 0f;

    public override States State => States.LightningEvade;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => new()
    {
        StatusEffect.Evade,
        StatusEffect.Strengthening
    };

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = 4;

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
        float value = _evadePerStack;
        _totalEvade += value;

        health.SetEvadePhys(health.EvadeMeleeDamage + value);
        health.SetEvadeMagic(health.ResistMagDamage + value);
    }

    public override void ExitState()
    {
        RemoveEvade();
        base.ExitState();
    }

    public override void ReduceStack()
    {
        RemoveEvade(_evadePerStack);
        currentStacksCount--;

        if (currentStacksCount <= 0) ExitState();
    }

    private void RemoveEvade(float value = -1)
    {
        if (value < 0) value = _totalEvade;

        health.SetEvadePhys(health.EvadeMeleeDamage - value);
        health.SetEvadeMagic(health.ResistMagDamage - value);

        _totalEvade -= value;
    }

    public override void UpdateState() { }
}