using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BleedingScraderDebuff : RefreshingState
{
    private Character _target;

    private float _baseDuration;
    private float _damage;
    private float _baseDamage;
    private float timerTick = 0;

    public override States State => States.BleedingScrader;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => throw new System.NotImplementedException();

    public BleedingScraderDebuff()
    {
        MaxStacksCount = 3;
        currentStacksCount = 1;
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _target = characterState.Character;
        _damage = damageToExit;
        _baseDamage = damageToExit;

        _baseDuration = durationToExit;
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            _baseDamage += _damage;
        }

        return true;
    }

    public override void UpdateState()
    {
        timerTick += Time.deltaTime;

        if (timerTick >= 1f)
        {
            BleedingDamage();
            timerTick = 0f;
        }
    }

    [Server]
    private void BleedingDamage()
    {
        Damage damage = new Damage()
        {
            Value = _damage,
            Type = DamageType.Physical,
        };

        _target.Health.TryTakeDamage(ref damage, null);
    }

    public override void ReduceStack()
    {
        if (duration < 0)
        {
            if (currentStacksCount > 0)
            {
                currentStacksCount--;
                _baseDamage -= _damage;
                duration = _baseDuration;
            }

            else ExitState();
        }
    }
}
