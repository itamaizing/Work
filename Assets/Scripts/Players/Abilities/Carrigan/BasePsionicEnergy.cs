using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class BasePsionicEnergy : Energy, IDamageable
{
    [SerializeField] private Character _player;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;

    private float _maxPsiEnergy;

    private float _reductionDamageMultiplier = 0.1f;

    private float _timeAbsorptionDamage;
    private float _startTimeAbsorptionDamage = 12f;

    private bool _isInternalPsiEnergy = false;

    private Coroutine _absorptionTimeCoroutine;

    public float CurrentPsiEnergy { get => CurrentValue; set => CurrentValue = value; }
    public bool IsAttackingPsiEnergyActive { get => _attackingPsionicEnergy.IsAttackingPsiEnergy; }

    public event Action<Damage, Skill> DamageTaken;

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if (damage.Value == 0)
            return true;

        if (CurrentValue > 0)
        {
            float reducingDamage = damage.Value * _reductionDamageMultiplier;

            if (CurrentValue < reducingDamage)
            {
                reducingDamage = CurrentValue;
            }
            
            damage.Value -= reducingDamage;
            CurrentValue -= damage.Value;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void ShowPhantomValue(Damage damage)
    {
    }

    public void ActivateAttackingEnergy()
    {
        _attackingPsionicEnergy.EnabledAttackingPsiEnergy();
    }

    public void IncreasePsiEnergy(float damageValue)
    {
        CurrentValue += damageValue;

        _isInternalPsiEnergy = true;
        RpcInternalPsiEnergyChanged(_isInternalPsiEnergy);

        if (_absorptionTimeCoroutine != null)
        {
            StopCoroutine(_absorptionTimeCoroutine);
            _absorptionTimeCoroutine = null;
            _timeAbsorptionDamage = _startTimeAbsorptionDamage;
        }

        _absorptionTimeCoroutine = StartCoroutine(AbsorptionTimeJob());
    }

    public void ReducingPsiEnergy(float reducingValue)
    {
        CurrentValue -= reducingValue;
    }

    private void Start()
    {
        _maxPsiEnergy = _player.Health.MaxValue;
        MaxValue = _maxPsiEnergy;

        _timeAbsorptionDamage = _startTimeAbsorptionDamage;

        _player.Health.Shields.Add(this);
    }

    private IEnumerator AbsorptionTimeJob()
    {
        while (_timeAbsorptionDamage > 0)
        {
            _timeAbsorptionDamage -= Time.deltaTime;
            if (_timeAbsorptionDamage < 0 || CurrentValue <= 0)
            {
                CurrentValue = 0;
                _isInternalPsiEnergy = false;

                RpcInternalPsiEnergyChanged(_isInternalPsiEnergy);
                yield break;
            }
            yield return null;
        }
    }

    #region ClientRpcMethods

    [ClientRpc]
    private void RpcInternalPsiEnergyChanged(bool value)
    {
        _isInternalPsiEnergy = value;
    }

    #endregion
}
