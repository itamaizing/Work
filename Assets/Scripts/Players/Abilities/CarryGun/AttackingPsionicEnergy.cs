using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AttackingPsionicEnergy : Energy
{
    [SerializeField] private Slider attackingPsionicsSlider;

    private const float _baseMaxAttackingPsiEnergy = 30f;
    private const float _timeAttackingPsiEnergy = 6f;

    [SyncVar(hook = nameof(OnEnergyUpdated))]
    private float _syncedEnergy;

    private float _currentMaxAttackingPsiEnergy;
    private float _remainingTime;
    private bool _isAttackingPsiActive = false;

    private Coroutine _attackingPsiEnergyCoroutine;

    public float MaxAttackingPsiEnergy => _currentMaxAttackingPsiEnergy;
    public bool IsAttackingPsiEnergy { get => _isAttackingPsiActive; set => _isAttackingPsiActive = value; }

    public event Action<float> OnEnergyChanged;

    #region Talent
    public void AttackingPsiIncrease(bool value)
    {
        if (value) _currentMaxAttackingPsiEnergy = _baseMaxAttackingPsiEnergy + 10f;
        else _currentMaxAttackingPsiEnergy = _baseMaxAttackingPsiEnergy;

        CurrentValue = Mathf.Min(CurrentValue, _currentMaxAttackingPsiEnergy);

        _maxValue = _currentMaxAttackingPsiEnergy;

        UpdateAttackingEnergyBar();
    }
    #endregion

    private void Start()
    {
        _currentMaxAttackingPsiEnergy = _baseMaxAttackingPsiEnergy;
        _maxValue = _currentMaxAttackingPsiEnergy;
        UpdateAttackingEnergyBar();
    }

    private void Update()
    {
        UpdateAttackingEnergyBar();
    }

    public float GetBonusDamage(float energySpent)
    {
        return Mathf.Floor(energySpent);
    }

    public int GetDispelCount(float energySpent)
    {
        return Mathf.FloorToInt(energySpent / 10f);
    }

    [Server]
    public void ReceiveAttackingEnergy(float transferAmount)
    {
        _remainingTime = _timeAttackingPsiEnergy;

        Add(transferAmount);
        CurrentValue = Mathf.Min(CurrentValue, _currentMaxAttackingPsiEnergy);

        _syncedEnergy = CurrentValue;

        if (_attackingPsiEnergyCoroutine != null)
            StopCoroutine(_attackingPsiEnergyCoroutine);

        _attackingPsiEnergyCoroutine = StartCoroutine(AttackingPsiEnergyJob());
    }

    private void OnEnergyUpdated(float oldValue, float newValue)
    {
        CurrentValue = newValue;
        UpdateAttackingEnergyBar();
    }

    private IEnumerator AttackingPsiEnergyJob()
    {
        while (_remainingTime > 0)
        {
            _remainingTime -= Time.deltaTime;
            yield return null;
        }

        CurrentValue = 0;
        _isAttackingPsiActive = false;

        OnEnergyChanged?.Invoke(CurrentValue);
        UpdateAttackingEnergyBar();
    }

    private void UpdateAttackingEnergyBar()
    {
        attackingPsionicsSlider.value = CurrentValue / _currentMaxAttackingPsiEnergy;
    }

    [Server]
    public void ExtendDuration(float amount)
    {
        if (CurrentValue <= 0) return;

        _remainingTime += amount;
    }
}
