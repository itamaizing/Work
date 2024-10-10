using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackingPsionicEnergy : Energy
{
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;

    private float _maxAttackingPsiEnergy = 30f;
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
            _basePsionicEnergy.CurrentPsiEnergy -= CurrentValue;
        }
        else
        {
            CurrentValue = _currentAttackingPsiEnergy;
            _basePsionicEnergy.CurrentPsiEnergy -= CurrentValue;
        }

        RpcAttackingPsiEnergyChanged(true, CurrentValue);

        if (_attackingPsiEnergyCoroutine != null)
        {
            StopCoroutine(_attackingPsiEnergyCoroutine);
            _attackingPsiEnergyCoroutine = null;
            _timeAttackingPsiEnergy = _startTimeAttackingPsiEnergy;
        }

        _attackingPsiEnergyCoroutine = StartCoroutine(AttackingPsiEnergyJob());
        /*
        _timeAttackingPsiEnergy = _startTimeAttackingPsiEnergy;

        _isAttackingPsiServer = true;

        if (_isAttackingPsiServer && CurrentValue < _maxAttackingPsiEnergy)
        {
            if (_maxAttackingPsiEnergy > _basePsionicEnergy.CurrentPsiEnergy)
            {
                CurrentValue += _basePsionicEnergy.CurrentPsiEnergy;

                if (CurrentValue > _maxAttackingPsiEnergy)
                {
                    CurrentValue = _maxAttackingPsiEnergy;
                }

                _basePsionicEnergy.CurrentPsiEnergy = 0;
                Debug.Log("AttackingPsiEnergy / if > curPsiEnergy / CurrentValue = " + CurrentValue);
            }
            else
            {
                CurrentValue += _maxAttackingPsiEnergy;

                if (CurrentValue > _maxAttackingPsiEnergy)
                {
                    CurrentValue = _maxAttackingPsiEnergy;
                }

                _basePsionicEnergy.CurrentPsiEnergy -= CurrentValue;
                Debug.Log("AttackingPsiEnergy / else / CurrentValue = " + CurrentValue);
            }

            RpcAttackingPsiEnergyChanged(_isAttackingPsiServer, CurrentValue);

            if (_attackingPsiEnergyCoroutine != null)
            {
                StopCoroutine(_attackingPsiEnergyCoroutine);
                _attackingPsiEnergyCoroutine = null;
                _timeAttackingPsiEnergy = _startTimeAttackingPsiEnergy;
            }

            _attackingPsiEnergyCoroutine = StartCoroutine(AttackingPsiEnergyJob());
        }
        */
    }

    private void Start()
    {
        MaxValue = 100f;
        Debug.Log("AttackingPsiEnergy / MaxValue = " + MaxValue);
    }

    private IEnumerator AttackingPsiEnergyJob()
    {
        while (_timeAttackingPsiEnergy > 0)
        {
            Debug.Log("AttackingPsiEnergy / AttackingPsiEnergyJob");
            _timeAttackingPsiEnergy -= Time.deltaTime;
            if (_timeAttackingPsiEnergy < 0)
            {
                RpcAttackingPsiEnergyChanged(false, 0f);

                CurrentValue = 0;

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

        _basePsionicEnergy.CurrentPsiEnergy -= _currentAttackingPsiEnergy;

        Debug.Log("AttackingPsiEnergy / _currentAttackingPsiEnergy = " + _currentAttackingPsiEnergy);
    }

    
}
