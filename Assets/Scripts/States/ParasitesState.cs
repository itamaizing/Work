using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ParasitesState : RefreshingState
{
    private const float TickInterval = 3f;
    private const float PercentDamage = 0.002f;

    private float _tickTimer;

    private List<StatusEffect> _effects = new() { StatusEffect.Poison };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Parasites;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public ParasitesState()
    {
        MaxStacksCount = 2;
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.personWhoMadeBuff = personWhoMadeBuff;

        duration = durationToExit;
        currentStacksCount = 1;

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

            float percentDamage = health.CurrentValue * PercentDamage * currentStacksCount;

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
        duration = time;

        if (currentStacksCount >= MaxStacksCount) return false;
        currentStacksCount++;

        return true;
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
    }
}