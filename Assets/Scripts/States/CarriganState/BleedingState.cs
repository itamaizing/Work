using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class BleedingState : RefreshingState
{
    private float _baseDamage;
    private float _percentDamage;
    private string _skillName;

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
        _baseDuration = durationToExit;
        _skillName = skillName;
        if (_skillName == "percentDamageNoReducing")
        {
            _percentDamage = damageToExit;
            _baseDamage = health.CurrentValue * _percentDamage;
        }
        else
        {
            _baseDamage = damageToExit;
        }

        _timeBetweenAttack = _startTimeBetweenAttack;

        health.IsDot = true;
        
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
    
    public override void ReduceStack()
    {
        currentStacksCount--;

        if (_skillName == "percentDamageNoReducing")
        {
            ExitState();
        }
        
        if (currentStacksCount <= 0)
        {
            characterState.StateIcons.RemoveItemByState(State);
            ExitState();
        }
        else
        {
            duration = _baseDuration;
        }
    }

    public override void ExitState()
    {
        health.IsDot = false;
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < 3)
        {
            currentStacksCount++;
        }
        if (_skillName == "percentDamageNoReducing")
        {
            duration = _baseDuration * currentStacksCount;
        }
        else
        {
            duration = _baseDuration;
        }
        return true;
    }
    
    private void BleedingDamage()
    {
        if (_skillName == "percentDamageNoReducing")
        {
            _baseDamage = health.CurrentValue * _percentDamage;
        }
        
        Damage damage = new Damage()
        {
            Value = _baseDamage,
            Type = DamageType.Physical,
            DamageKey = "bleeding"
        };
        if(characterState.isServer)
            health.TryTakeDamage(ref damage, null);
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
