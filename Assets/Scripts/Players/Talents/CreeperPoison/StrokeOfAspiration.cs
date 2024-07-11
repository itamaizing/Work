using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrokesOfAspiration : MonoBehaviour
{
    [SerializeField] private Character _dad;
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private CreeperStrike _creeperStrike;

    private float _maxHitCount = 2;
    private float _currentHitCount;

    private const float _timeBetweenAttack = 0.1f;
    private const float _decreaseCooldownTime = 0.3f;

    private GameObject _currentTarget;
    private GameObject _lastTarget;

    private Coroutine _useTalentCoroutine;
    public Coroutine StartJobTalentCoroutine;

    private void Awake()
    {
        _currentHitCount = _maxHitCount;
        InitializationAbilities();
    }

    public IEnumerator StartJobTalent()
    {
        _currentHitCount--;
        UseTalent();
        yield return null;
    }

    private void UseTalent()
    {
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
        _poisonBall = _dad.GetComponentInChildren<PoisonBall>();
        _spitPoison = _dad.GetComponentInChildren<SpitPoison>();
        _creeperStrike = _dad.GetComponentInChildren<CreeperStrike>();
    }


}
