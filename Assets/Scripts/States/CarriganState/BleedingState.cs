using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class BleedingStateStacking : RefreshingStateStacking
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

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _baseDuration = durationToExit;
        _baseDamage = damageToExit;

        _timeBetweenAttack = _startTimeBetweenAttack;

        SetMaxStacks(3);
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
            
            ExitState();
        }
        else
        {
            RemainingDuration = _baseDuration;
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
        RemainingDuration = _baseDuration;
        
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
    
    
}
