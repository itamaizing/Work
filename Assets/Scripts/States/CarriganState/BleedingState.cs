using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BleedingState : AbstractCharacterState
{
    private Character _target;

    private float _baseDamage = 6.0f;

    private float _duration;
    private float _baseDuration;

    private float _timeBetweenAttack;
    private float _startTimeBetweenAttack = 1.0f;

    private List<StatusEffect> _effects = new List<StatusEffect>();
    public override States State => States.Bleeding;

    public override StateType Type => StateType.Physical;

    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _target = _characterState.Character;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        _timeBetweenAttack = _startTimeBetweenAttack;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
        
        _timeBetweenAttack -= Time.deltaTime;
        if (_timeBetweenAttack <= 0)
        {
            BleedingDamage();
            _timeBetweenAttack = _startTimeBetweenAttack;
        }
    }

    public override void ExitState()
    {
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        _duration = _baseDuration;
        return true;
    }

    private void BleedingDamage()
    {
        Damage damage = new Damage()
        {
            Value = _baseDamage,
            Type = DamageType.Physical,
            Range = AttackRangeType.Inner,
        };

        _target.Health.TryTakeDamage(ref damage, null);
    }
}
