using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BasePsionicEnergy : Resource, IDamageable
{
    [SerializeField] private Character _player;
    [SerializeField] private Slider basePsionicsSlider;
    [SerializeField] private PsionicEnergySkill psionicEnergySkill;

    private const float DamageToPsiConversionRate = 0.1f;

    private float _psionicaDecayTime;
    private Coroutine _energyDecayCoroutine;

    public event Action<Damage, Skill> DamageTaken;
    public event Action<float> OnEnergyChanged;

    private void Start()
    {
        if (_player == null) return;

        _psionicaDecayTime = psionicEnergySkill.CooldownTime;

        _player.Health.Shields.Add(this);

        if (isServer)
        {
            _maxValue = _player.Health.MaxValue;
            CurrentValue = 0f;
        }

        _player.Health.MaxValueChanged += OnHealthMaxChanged;
    }

    private void OnEnable()
    {
        if (_player != null && _player.DamageTracker != null)
        {
            _player.DamageTracker.OnDamageTracked -= OnDamageDealt;
            _player.DamageTracker.OnDamageTracked += OnDamageDealt;
        }
    }

    private void OnDisable()
    {
        if (_player != null && _player.DamageTracker != null)
            _player.DamageTracker.OnDamageTracked -= OnDamageDealt;
    }

    private void OnHealthMaxChanged(float oldValue, float newValue)
    {
        if (!isServer) return;

        _maxValue = newValue;

        if (CurrentValue > _maxValue)
            CurrentValue = _maxValue;
    }

    private void OnDamageDealt(Damage damage, GameObject target)
    {
        if (!isServer) return;

        if (damage.Type != DamageType.Physical) return;
        if (psionicEnergySkill == null || !psionicEnergySkill.IsPsiEnergyActive) return;

        float energyGain = damage.Value * DamageToPsiConversionRate;

        CurrentValue = Mathf.Min(CurrentValue + energyGain, MaxValue);

        if (_energyDecayCoroutine != null)
            StopCoroutine(_energyDecayCoroutine);

        _energyDecayCoroutine = StartCoroutine(EnergyDecayCoroutine());
    }

    private IEnumerator EnergyDecayCoroutine()
    {
        yield return new WaitForSeconds(_psionicaDecayTime);

        if (isServer)
            CurrentValue = 0f;
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if (damage.Value == 0) return true;

        if (CurrentValue > 0)
        {
            float absorbAmount = Mathf.Min(CurrentValue, damage.Value);

            damage.Value -= absorbAmount;
            CurrentValue -= absorbAmount;

            return true;
        }

        return false;
    }

    protected override void HookValueChanged(float oldValue, float newValue)
    {
        base.HookValueChanged(oldValue, newValue);
        UpdatePsionicaBar();
        OnEnergyChanged?.Invoke(newValue);
    }

    private void UpdatePsionicaBar()
    {
        if (basePsionicsSlider == null || MaxValue <= 0f)
        {
            if (basePsionicsSlider != null)
                basePsionicsSlider.value = 0f;
            return;
        }

        basePsionicsSlider.value = CurrentValue / MaxValue;
    }

    public override void Add(float value)
    {
        if (psionicEnergySkill == null || !psionicEnergySkill.IsPsiEnergyActive) return;

        base.Add(value);
    }

    [Server]
    public void AddAndResetDecay(float value)
    {
        if (psionicEnergySkill == null || !psionicEnergySkill.IsPsiEnergyActive)
            return;

        CurrentValue = Mathf.Min(CurrentValue + value, MaxValue);

        if (_energyDecayCoroutine != null)
            StopCoroutine(_energyDecayCoroutine);

        _energyDecayCoroutine = StartCoroutine(EnergyDecayCoroutine());
    }

    public void ShowPhantomValue(Damage phantomValue)
    {
    }
}