using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BasePsionicEnergy : Resource, IDamageable
{
    [SerializeField] private Character _player;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private Slider basePsionicsSlider;

    private const float BasePsionicaThreshold = 30f;
    private const float BaseSliderFillPercent = 0.3f;
    private const float RemainingSliderFillPercent = 0.7f;
    private const float PsionicaDecayTime = 12f;
    private const float DamageToPsiConversionRate = 1f;

    private bool _isInternalPsiEnergy = false;
    private Coroutine _energyDecayCoroutine;

    public bool IsAttackingPsiEnergyActive => _attackingPsionicEnergy.IsAttackingPsiEnergy;

    public event Action<Damage, Skill> DamageTaken;
    public event Action<float> OnEnergyChanged;

    private void Start()
    {
        if (_player != null)
        {
            MaxValue = _player.Data.GetAttributeValue(AttributeNames.Health);
            _player.Health.Shields.Add(this);

            if (_player.DamageTracker != null)
            {
                _player.DamageTracker.OnDamageTracked += OnDamageDealt;
            }
        }
    }

    private void Update()
    {
        UpdatePsionicaBar();
    }

    private void OnDestroy()
    {
        if (_player != null && _player.DamageTracker != null)
        {
            _player.DamageTracker.OnDamageTracked -= OnDamageDealt;
        }
    }

    private void OnDamageDealt(Damage damage, GameObject target)
    {
        if (damage.Type == DamageType.Physical)
        {
            float energyGain = damage.Value * DamageToPsiConversionRate;
            Add(energyGain);
            CurrentValue = Mathf.Min(CurrentValue, MaxValue);

            OnEnergyChanged?.Invoke(CurrentValue);

            bool wasInternalEnergy = _isInternalPsiEnergy;
            _isInternalPsiEnergy = CurrentValue > 0;

            if (wasInternalEnergy != _isInternalPsiEnergy)
            {
                RpcInternalPsiEnergyChanged(_isInternalPsiEnergy);
            }

            if (_energyDecayCoroutine != null)
            {
                StopCoroutine(_energyDecayCoroutine);
            }
            _energyDecayCoroutine = StartCoroutine(EnergyDecayCoroutine());

            UpdatePsionicaBar();
        }
    }

    public void UsePsiEnergy(float value)
    {
        TryUse(value);
        OnEnergyChanged?.Invoke(CurrentValue);
        UpdatePsionicaBar();
    }

    public void PsiAbsorption(ref float modifiedDamage)
    {
        if (CurrentValue > 0)
        {
            float absorptionAmount = Mathf.Min(CurrentValue, modifiedDamage);
            UsePsiEnergy(absorptionAmount);
            modifiedDamage -= absorptionAmount * 0.1f;
        }
    }

    private void UpdatePsionicaBar()
    {
        float normalizedValue = 0f;

        if (CurrentValue <= BasePsionicaThreshold)
        {
            normalizedValue = (CurrentValue / BasePsionicaThreshold) * BaseSliderFillPercent;
        }
        else
        {
            float remainingValue = (CurrentValue - BasePsionicaThreshold) / (MaxValue - BasePsionicaThreshold);
            normalizedValue = BaseSliderFillPercent + (remainingValue * RemainingSliderFillPercent);
        }

        basePsionicsSlider.value = normalizedValue;
    }

    private IEnumerator EnergyDecayCoroutine()
    {
        yield return new WaitForSeconds(PsionicaDecayTime);
        CurrentValue = 0;
        OnEnergyChanged?.Invoke(CurrentValue);
        _isInternalPsiEnergy = false;
        UpdatePsionicaBar();
        RpcInternalPsiEnergyChanged(_isInternalPsiEnergy);
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if (damage.Value == 0)
            return true;

        if (CurrentValue > 0)
        {
            float absorbingDamage = Mathf.Min(CurrentValue, damage.Value);
            damage.Value -= absorbingDamage * 0.1f; 
            UsePsiEnergy(absorbingDamage);

            _isInternalPsiEnergy = CurrentValue > 0;
            RpcInternalPsiEnergyChanged(_isInternalPsiEnergy);

            UpdatePsionicaBar();
            return true;
        }

        return false;
    }

    public void ConvertToAttackingEnergy(float amount)
    {
        float transferAmount = Mathf.Min(CurrentValue, amount);
        if (transferAmount > 0)
        {
            UsePsiEnergy(transferAmount);
            _attackingPsionicEnergy.ReceiveAttackingEnergy(transferAmount);
        }
    }

    [ClientRpc]
    private void RpcInternalPsiEnergyChanged(bool value)
    {
        _isInternalPsiEnergy = value;
    }

    public void ShowPhantomValue(Damage phantomValue)
    {
        throw new NotImplementedException();
    }
}
