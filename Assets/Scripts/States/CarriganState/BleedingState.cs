using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class BleedingState : AbstractCharacterState
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

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _target = characterState.Character;
;
        _baseDuration = durationToExit;
        _baseDamage = damageToExit;

        _timeBetweenAttack = _startTimeBetweenAttack;

        _target.Health.IsDot = true;
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

    protected override void ExitState()
    {
        _target.Health.IsDot = false;
    }

    /*public override bool Stack(float time)
    {
        duration = _baseDuration;
        return true;
    }*/

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
}
