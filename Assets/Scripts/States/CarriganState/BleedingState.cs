using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class BleedingState : RefreshingState
{
    private Character _target;
    
    private float _baseDamage;

    private float _baseDuration;
    
    private float _timeBetweenAttack;
    private float _startTimeBetweenAttack = 1.0f;

    private List<StatusEffect> _effects = new List<StatusEffect>();
    public override States State => States.Bleeding;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _target = characterState.Character;
;
        _baseDuration = durationToExit;
        _baseDamage = damageToExit;

        _timeBetweenAttack = _startTimeBetweenAttack;

        _target.Health.IsDot = true;
        
        MaxStacksCount = 3;
        currentStacksCount = 1;
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
        _target.Health.IsDot = false;
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < 3)
        {
            currentStacksCount++;
            duration = _baseDuration;
            return true;
        }
        duration = _baseDuration;
        return true;
    }

    [Server]
    private void BleedingDamage()
    {
        Damage damage = new Damage()
        {
            Value = _baseDamage,
            Type = DamageType.Physical,
        };

        _target.Health.TryTakeDamage(ref damage, null);
    }
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        else
            Stack(duration);

        return this;
    }
}
