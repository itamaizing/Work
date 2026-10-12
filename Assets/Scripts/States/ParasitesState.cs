using System.Collections.Generic;
using UnityEngine;

public class ParasitesStateStacking : StateStackingRefreshing, ITickableState
{
    private const float TickIntervalConst = 3f;
    private const float PercentDamage = 0.02f;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Poison };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Parasites;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public float TickInterval => TickIntervalConst;

    public ParasitesStateStacking()
    {
        SetMaxStacks(2);
    }
    
    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
    }

    public override bool Stack(float time)
    {
        RemainingDuration = time;

        if (CurrentStacksCount >= MaxStacksCount) return false;
        CurrentStacksCount++;

        return true;
    }

    public void Tick()
    {
        if (!characterState.isServer) return;
        if (health == null) return;

        float percentDamage = health.CurrentValue * PercentDamage * CurrentStacksCount;

        Damage damage = new Damage
        {
            Value = percentDamage,
            Type = DamageType.Physical
        };

        health.TryTakeDamage(ref damage, skill);
    }

    protected override void OnExit()
    {
        CurrentStacksCount = 0;
    }
}