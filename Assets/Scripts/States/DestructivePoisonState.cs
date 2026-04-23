using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class DestructivePoisonState : RefreshingState
{
    private Character _target;
    private Health _health;

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

        _target = character.Character;
        _health = _target.Health;

        duration = durationToExit;
        currentStacksCount = 1;

        _tickTimer = TickInterval;
    }

    public override void UpdateState()
    {
        _tickTimer -= Time.deltaTime;

        if (_tickTimer <= 0f)
        {
            _tickTimer = TickInterval;

            if (_health == null || _target == null || _target.IsDead)
                return;

            float damageValue = DamagePerTick * currentStacksCount;

            if (NetworkServer.active)
            {
                Damage damage = new Damage
                {
                    Value = damageValue,
                    Type = DamageType.Physical,
                    School = Schools.Earth
                };

                _health.TryTakeDamage(ref damage, null);
            }

            _health.barCharacter.PreviewDoTTick(damageValue);
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