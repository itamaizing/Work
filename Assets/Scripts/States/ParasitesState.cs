using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ParasitesStateStacking : StateStackingRefreshing
{
    private const float TickInterval = 3f;
    private const float PercentDamage = 0.002f;

    private float _tickTimer;

    private List<StatusEffect> _effects = new() { StatusEffect.Poison };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Parasites;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public ParasitesStateStacking()
    {
        SetMaxStacks(2);
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.sourceCaster = personWhoMadeBuff;

        RemainingDuration = durationToExit;
        CurrentStacksCount = 1;

        _tickTimer = TickInterval;
    }

    public override void UpdateState()
    {
        if (!NetworkServer.active) return;
        if (health == null) return;

        _tickTimer -= Time.deltaTime;

        if (_tickTimer <= 0f)
        {
            _tickTimer = TickInterval;

            float percentDamage = health.CurrentValue * PercentDamage * CurrentStacksCount;

            Damage damage = new Damage
            {
                Value = percentDamage,
                Type = DamageType.Physical
            };

            health.TryTakeDamage(ref damage, skill);
        }
    }

    public override bool Stack(float time)
    {
        RemainingDuration = time;

        if (CurrentStacksCount >= MaxStacksCount) return false;
        CurrentStacksCount++;

        return true;
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
    }
}