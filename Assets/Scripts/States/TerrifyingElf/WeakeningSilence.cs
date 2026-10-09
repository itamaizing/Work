using System.Collections.Generic;
using UnityEngine;

public class WeakeningSilence : StateStacking, ITickableState
{
    private const int MaxStacks = 6;
    private const float DamagePerStack = 3f;

    public override States State => States.WeakeningSilence;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => new List<StatusEffect> { StatusEffect.Poison };

    public float TickInterval => 1f;

    public WeakeningSilence()
    {
        SetMaxStacks(MaxStacks);
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName) { }

    public override void UpdateState() { }

    public override void GlobalUpdate()
    {
        UpdateState();
        ProcessTick();

        RemainingDuration -= Time.deltaTime;
        if (RemainingDuration <= 0f) ExitState();
    }

    public override void ExitState()
    {
        base.ExitState();
        CurrentStacksCount = 0;
    }

    public void Tick()
    {
        if (characterState == null || characterState.Character == null || !characterState.isServer)
            return;

        Damage damage = new Damage
        {
            Value = DamagePerStack * CurrentStacksCount,
            Type = DamageType.Magical
        };

        health.TryTakeDamage(ref damage, null);
    }
}