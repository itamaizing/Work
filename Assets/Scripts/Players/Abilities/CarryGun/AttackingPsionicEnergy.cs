using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AttackingPsionicEnergy : Resource
{
    [SerializeField] private Slider attackingPsionicsSlider;
    [SerializeField] private Character _player;

    private const float _baseMaxAttackingPsiEnergy = 30f;
    private const float _timeAttackingPsiEnergy = 3f;
    
    [SyncVar]
    private bool _isAttackingPsiActive = false;

    private float _currentMaxAttackingPsiEnergy;
    [SyncVar]
    private double _expiresAt;

    public float RemainingTime => _isAttackingPsiActive
        ? Mathf.Max(0f, (float)(_expiresAt - NetworkTime.time))
        : 0f;

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
        _expiresAt = NetworkTime.time + _timeAttackingPsiEnergy;

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
        while (NetworkTime.time < _expiresAt)
            yield return null;

        _attackingPsiEnergyCoroutine = null;
        AttackingPsiEnergyReset();
    }

    private void AttackingPsiEnergyReset()
    {
        if (!isServer) return;

        if (_attackingPsiEnergyCoroutine != null)
        {
            StopCoroutine(_attackingPsiEnergyCoroutine);
            _attackingPsiEnergyCoroutine = null;
        }

        _expiresAt = 0;
        CurrentValue = 0;
        _isAttackingPsiActive = false;

        UpdateAttackingEnergyBar();
    }

    private void UpdateAttackingEnergyBar()
    {
        if (attackingPsionicsSlider == null || _currentMaxAttackingPsiEnergy <= 0f) return;
        attackingPsionicsSlider.value = CurrentValue / _currentMaxAttackingPsiEnergy;
    }

    public void SetBarWidth(float width)
    {
        if (attackingPsionicsSlider == null || width <= 0f) return;

        var rect = (RectTransform)attackingPsionicsSlider.transform;
        if (!Mathf.Approximately(rect.rect.width, width))
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    [Server]
    public void ExtendDuration(float amount)
    {
        if (amount <= 0f
            || !_isAttackingPsiActive
            || CurrentValue <= 0f
            || _expiresAt <= NetworkTime.time)
            return;

        _expiresAt += amount;
    }
}
