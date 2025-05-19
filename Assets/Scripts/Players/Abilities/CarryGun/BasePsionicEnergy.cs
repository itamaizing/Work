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
    [SerializeField] private bool isPsionicsTalentOne = false;

    private const float BasePsionicaThreshold = 30f;
    private const float BaseSliderFillPercent = 0.3f;
    private const float RemainingSliderFillPercent = 0.7f;
    private const float PsionicaDecayTime = 12f;
    private const float DamageToPsiConversionRate = 0.2f;

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

            if (_player.DamageTracker != null) _player.DamageTracker.OnDamageTracked += OnDamageDealt;
            if (_player.Health != null) _player.Health.OnBeforeTakeDamage += HandleIncomingDamage;
        }
    }

    private void Update()
    {
        UpdatePsionicaBar();
    }

    private void OnDestroy()
    {
        if (_player != null && _player.DamageTracker != null) _player.DamageTracker.OnDamageTracked -= OnDamageDealt;
        if (_player.Health != null) _player.Health.OnBeforeTakeDamage -= HandleIncomingDamage;
    }

    private void OnDamageDealt(Damage damage, GameObject target)
    {
        if (!isPsionicsTalentOne) return;

        if (damage.Type == DamageType.Physical)
        {
            float energyGain = damage.Value * DamageToPsiConversionRate;
            Add(energyGain);
            CurrentValue = Mathf.Min(CurrentValue, MaxValue);

            RpcOnEnergyChanged(CurrentValue);

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

    public void AddAndResetDecay(float value)
    {
        if (!isPsionicsTalentOne) return;

        Add(value);
        CurrentValue = Mathf.Min(CurrentValue, MaxValue);

        if (isServer)
        {
            RpcOnEnergyChanged(CurrentValue);

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
        }

        UpdatePsionicaBar();
    }


    public void UsePsiEnergy(float value)
    {
        TryUse(value);
        RpcOnEnergyChanged(CurrentValue);
        UpdatePsionicaBar();
    }

    private void HandleIncomingDamage(Damage damage, Skill skill)
    {
        if (damage.Value <= 0 || CurrentValue <= 0) return;

        float absorptionAmount = Mathf.Min(CurrentValue, damage.Value);
        UsePsiEnergy(absorptionAmount);

        float reduced = absorptionAmount * 0.5f;
        damage.Value -= reduced;

        damage.Value = Mathf.Max(damage.Value, 0f);
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

        Debug.Log($"Текущая пси энергия: {CurrentValue}");
    }

    private IEnumerator EnergyDecayCoroutine()
    {
        yield return new WaitForSeconds(PsionicaDecayTime);
        CurrentValue = 0;
        RpcOnEnergyChanged(CurrentValue);
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
            damage.Value -= absorbingDamage * 0.5f; 
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

    [ClientRpc]
    private void RpcOnEnergyChanged(float value)
    {
        CurrentValue = value;
        OnEnergyChanged?.Invoke(value);
    }

    public void ShowPhantomValue(Damage phantomValue)
    {
        throw new NotImplementedException();
    }

    #region Talents

    public void PsionicsTalentOne(bool value)
    {
        isPsionicsTalentOne = value;
    }

    #endregion
}
