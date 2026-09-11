using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BleedingStateStackingCarry : RefreshingStateStacking
{
    private float _baseDamage;
    private float _percentDamage;
    private const float MaxDuration = 21f;

    private float _timeBetweenAttack;
    private float _startTimeBetweenAttack = 1.0f;

    private List<StatusEffect> _effects = new List<StatusEffect>();
    public override States State => States.BleedingCarry;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    protected override void BaseInit(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        abilities = character.Character.Abilities;

        parameters[StateParameter.DamageToExit] = 1000;
        this.sourceCaster = personWhoMadeBuff;
    }

    

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _timeBetweenAttack = _startTimeBetweenAttack;
        
        _percentDamage = damageToExit;
        RemainingDuration = Mathf.Min(durationToExit, MaxDuration);

        _timeBetweenAttack = _startTimeBetweenAttack;

        SetMaxStacks(1);
        currentStacksCount = 1;
    }

    public override bool Stack(float time)
    {
        RemainingDuration = Mathf.Min(RemainingDuration + time, MaxDuration);

        return true;
    }

    public override void UpdateState()
    {
        _timeBetweenAttack -= Time.deltaTime;
        if (_timeBetweenAttack <= 0)
        {
            BleedingDamage();
            characterState.Character.Health.barCharacter.PreviewDoTTick(_baseDamage);
            _timeBetweenAttack = _startTimeBetweenAttack;
        }
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        characterState.RemoveState(this);
    }

    private void BleedingDamage()
    {
        _baseDamage = health.MaxValue * _percentDamage;

        Damage damage = new Damage()
        {
            Value = _baseDamage,
            Type = DamageType.DOTPhys,
            DamageKey = "bleeding"
        };
        if (characterState.isServer)
            health.TryTakeDamage(ref damage, null);
    }
}