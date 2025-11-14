using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BleedingScraderDebuff : AbstractCharacterState
{
    private Character _target;

    private float _duration;
    private float _baseDuration;
    private float _damage;
    private float timerTick = 0;

    public override States State => States.BleedingScrader;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => throw new System.NotImplementedException();

    public BleedingScraderDebuff()
    {
        MaxStacksCount = 3;
        CurrentStacksCount = 1;
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _target = _characterState.Character;
        _damage = damageToExit;

        _baseDuration = durationToExit;
        _duration = durationToExit;
    }

    public override void ExitState()
    {
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            _damage++;
        }

        return true;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        timerTick += Time.deltaTime;

        if (timerTick >= 1f)
        {
            BleedingDamage();
            timerTick = 0f;
        }

        if (_duration < 0)
        {
            if (CurrentStacksCount > 0)
            {
                CurrentStacksCount--;
                _damage--;
                _duration = _baseDuration;
            }

            else ExitState();
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
}
