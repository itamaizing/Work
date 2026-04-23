using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class BleedingState : RefreshingState
{
    private Character _target;
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
        _baseDuration = durationToExit;

        _timeBetweenAttack = _startTimeBetweenAttack;

        _target.Health.IsDot = true;
    }

    public override void UpdateState()
    {
        _timeBetweenAttack -= Time.deltaTime;

        if (_timeBetweenAttack <= 0)
        {
            if (NetworkServer.active) BleedingDamage();
            float previewDamage = _target.Health.MaxValue * 0.003f;
            characterState.Character.Health.barCharacter.PreviewDoTTick(previewDamage);

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
        duration = _baseDuration;
        return true;
    }

    [Server]
    private void BleedingDamage()
    {
        if (_target == null || _target.IsDead)
            return;

        float bleedDamage = _target.Health.MaxValue * 0.003f;

        Damage damage = new Damage()
        {
            Value = bleedDamage,
            Type = DamageType.Physical,
        };

        _target.Health.TryTakeDamage(ref damage, null);
    }
}
