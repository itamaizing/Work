using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrokesOfAspiration : Talent
{
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private CreeperStrike _creeperStrike;

    private float _maxHitCount = 2;
    private float _currentHitCount;

    private const float _timeBetweenAttack = 0.1f;
    private const float _decreaseCooldownTime = 0.3f;

    private GameObject _currentTarget;
    private GameObject _lastTarget;

    private void Start()
    {
        _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_timeBetweenAttack);
        isActive = true;
        _currentHitCount = _maxHitCount;
        InitializationAbilities();
    }

    public override void Enter()
    {
        Debug.Log(_creeperStrike.Buff.AttackSpeed.Multiplier);
        UseTalent();
    }

    public override void Exit()
    {
        isActive = false;
        _creeperStrike.Buff.AttackSpeed.IncreasePercentage(1.0f);
    }

    private void UseTalent()
    {
        _currentHitCount--;
        if (_creeperStrike.CurrentTarget != null)
        {
            _currentTarget = _creeperStrike.CurrentTarget;

            if (_currentHitCount <= 0 && _lastTarget == _currentTarget)
            {
                float updateRemainingCooldownTimeForPoisonBall = _poisonBall.RemainingÑooldownTime - _decreaseCooldownTime;
                _poisonBall.ReductionSetCooldown(updateRemainingCooldownTimeForPoisonBall);

                float updateRemainingCooldownTimeForSpitPoison = _spitPoison.RemainingÑooldownTime - _decreaseCooldownTime;
                _spitPoison.ReductionSetCooldown(updateRemainingCooldownTimeForSpitPoison); 
            }
            else
            {
                _lastTarget = _currentTarget; 
            }
        }

        if (_currentHitCount == 0)
            _currentHitCount = _maxHitCount;
    }

    private void InitializationAbilities()
    {
        _poisonBall = character.GetComponentInChildren<PoisonBall>();
        _spitPoison = character.GetComponentInChildren<SpitPoison>();
        _creeperStrike = character.GetComponentInChildren<CreeperStrike>();
    }
}
