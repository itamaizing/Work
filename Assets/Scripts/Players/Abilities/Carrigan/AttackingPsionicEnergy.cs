using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackingPsionicEnergy : Energy
{
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;

    private float _maxAttackingPsiEnergy = 30f;
    private float _maxValueEnergyBar = 100f;
    private float _currentAttackingPsiEnergy;

    private float _timeAttackingPsiEnergy;
    private float _startTimeAttackingPsiEnergy = 6f;

    private bool _isAttackingPsiClient = false;
    
    private Coroutine _attackingPsiEnergyCoroutine;

    public float CurrentAttackingPsiEnergy { get => CurrentValue; set => CurrentValue = value; }
    public float MaxAttackingPsiEnergy { get => _maxAttackingPsiEnergy; set => _maxAttackingPsiEnergy = value; }
    public bool IsAttackingPsiEnergy { get => _isAttackingPsiClient; set => _isAttackingPsiClient = value; }

    public void EnabledAttackingPsiEnergy()
    {
        _timeAttackingPsiEnergy = _startTimeAttackingPsiEnergy;

        _currentAttackingPsiEnergy += _basePsionicEnergy.CurrentPsiEnergy;

        if (_currentAttackingPsiEnergy > _maxAttackingPsiEnergy)
        {
            CurrentValue = _maxAttackingPsiEnergy;
        }
        else
        {
            CurrentValue = _currentAttackingPsiEnergy;
        }

        _basePsionicEnergy.ReducingPsiEnergy(CurrentValue);

        RpcAttackingPsiEnergyChanged(true, CurrentValue);

        if (_attackingPsiEnergyCoroutine != null)
        {
            StopCoroutine(_attackingPsiEnergyCoroutine);
            _attackingPsiEnergyCoroutine = null;
            _timeAttackingPsiEnergy = _startTimeAttackingPsiEnergy;
        }

        _attackingPsiEnergyCoroutine = StartCoroutine(AttackingPsiEnergyJob());
    }

    private void Start()
    {
        MaxValue = _maxValueEnergyBar;
    }

    private IEnumerator AttackingPsiEnergyJob()
    {
        while (_timeAttackingPsiEnergy > 0)
        {
            _timeAttackingPsiEnergy -= Time.deltaTime;
            if (_timeAttackingPsiEnergy < 0 || CurrentValue <= 0)
            {
                CurrentValue = 0;
                _currentAttackingPsiEnergy = 0;

                RpcAttackingPsiEnergyChanged(false, 0f);

                yield break;
            }   
            yield return null;   
        }
    }

    [ClientRpc]
    private void RpcAttackingPsiEnergyChanged(bool isAttackingPsionicEnergy, float currentAttackingPsiEnergy)
    {
        _isAttackingPsiClient = isAttackingPsionicEnergy;

        _currentAttackingPsiEnergy = currentAttackingPsiEnergy;
    }

    
}
