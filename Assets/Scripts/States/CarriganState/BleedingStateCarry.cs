using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BleedingStateCarry : RefreshingState
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

        this.damageToExit = 1000;
        this.personWhoMadeBuff = personWhoMadeBuff;
    }

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        else
            Stack(durationToExit);

        return this;
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _timeBetweenAttack = _startTimeBetweenAttack;
        
        _percentDamage = damageToExit;
        duration = Mathf.Min(durationToExit, MaxDuration);

        _timeBetweenAttack = _startTimeBetweenAttack;

        MaxStacksCount = 1;
        currentStacksCount = 1;
    }

    public override bool Stack(float time)
    {
        duration = Mathf.Min(duration + time, MaxDuration);

        _timeBetweenAttack = _startTimeBetweenAttack;

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