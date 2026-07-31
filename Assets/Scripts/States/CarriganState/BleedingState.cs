using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class BleedingState : RefreshingState
{
    private float _baseDamage;
    private float _percentDamage;

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
        _baseDamage = damageToExit;

        _timeBetweenAttack = _startTimeBetweenAttack;

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
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < 3)
        {
            currentStacksCount++;
        }
        duration = _baseDuration;
        
        return true;
    }
    
    private void BleedingDamage()
    {
        Damage damage = new Damage()
        {
            Value = _baseDamage,
            Type = DamageType.DOTPhys,
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
