using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AttackingPsionicEnergy : BasePsionicEnergy
{
    [SerializeField] private Slider attackingPsionicsSlider;
    [SerializeField] private Character _player;

    private const float _baseMaxAttackingPsiEnergy = 30f;
    private const float _timeAttackingPsiEnergy = 3f;
    
    [SyncVar]
    private bool _isAttackingPsiActive = false;

    private float _currentMaxAttackingPsiEnergy;
    private float _remainingTime;

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

    private void OnEnable()
    {
        _player.Reset += AttackingPsiEnergyReset;
    }

    private void OnDisable()
    {
        _player.Reset -= AttackingPsiEnergyReset;
    }

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

        _isAttackingPsiActive = true;

        if (_attackingPsiEnergyCoroutine != null)
            StopCoroutine(_attackingPsiEnergyCoroutine);

        _attackingPsiEnergyCoroutine = StartCoroutine(AttackingPsiEnergyJob());
    }
    
    protected override void HookValueChanged(float oldValue, float newValue)
    {
        base.HookValueChanged(oldValue, newValue);
        OnEnergyChanged?.Invoke(newValue);
        UpdateAttackingEnergyBar();
    }

    private IEnumerator AttackingPsiEnergyJob()
    {
        while (_remainingTime > 0)
        {
            _remainingTime -= Time.deltaTime;
            yield return null;
        }

        AttackingPsiEnergyReset();
    }

    private void AttackingPsiEnergyReset()
    {
        CurrentValue = 0;
        _isAttackingPsiActive = false;

        UpdateAttackingEnergyBar();
    }

    private void UpdateAttackingEnergyBar()
    {
        if (attackingPsionicsSlider == null || _currentMaxAttackingPsiEnergy <= 0f) return;
        attackingPsionicsSlider.value = CurrentValue / _currentMaxAttackingPsiEnergy;
    }

    [Server]
    public void ExtendDuration(float amount)
    {
        if (CurrentValue <= 0) return;

        _remainingTime += amount;
    }
}