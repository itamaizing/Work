using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class DestructivePoisonState : RefreshingState
{
    private const float TickInterval = 1f;
    private const float DamagePerTick = 1f;

    private float _tickTimer;

    private List<StatusEffect> _effects = new() { StatusEffect.Poison };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.DestructivePoison;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;
    public override Schools Schools => Schools.Earth;
    
    public DestructivePoisonState()
    {
        MaxStacksCount = 3;
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

            float damageValue = DamagePerTick * currentStacksCount;

            Damage damage = new Damage
            {
                Value = damageValue,
                Type = DamageType.Physical,
                School = Schools.Earth
            };

            health.TryTakeDamage(ref damage, skill);
        }
    }

    public override bool Stack(float time)
    {
        duration = time;

        if (currentStacksCount >= MaxStacksCount)
            return false;

        currentStacksCount++;
        return true;
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
    }
}