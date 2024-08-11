using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Skill : MonoBehaviour
{
    [Header("AbilitieInfo")]
    [SerializeField] private AbilityInfo _abilityInfo;
    [Header("Settings")]
    [SerializeField] protected bool _isAutoAttack;
    [SerializeField] protected float _manaCost = 0f;
    [SerializeField] protected float _cooldownTime = 0f;
    [SerializeField] protected float _castDeley = 0f;
    [SerializeField] protected Schools _abilitySchool;
    [SerializeField] protected AbilityForm _abilityForm;
    [SerializeField] protected LayerMask _targetsLayers;
    [Header("Charge settings")]
    [SerializeField] protected bool _isUseCharges;
    [SerializeField] protected bool _chargesHaveSeparateCooldown;
    [SerializeField] protected int _maxCharges;
    [SerializeField] protected float _chargeCooldown;

    protected HeroComponent _hero;
    protected bool _isCanCancle = true;
    protected int _currentChargers;
    protected Coroutine _preparingCoroutine;
    protected Coroutine _castingCoroutine;
    protected Coroutine _cooldownJob;
    protected Coroutine _rechargeJob;
    protected Coroutine _castDeleyJob;

    private float _remaining—ooldownTime;
    private StatsBuff _statsBuff = new StatsBuff();
    private Coroutine _actionWrapperForPreparingCoroutine;
    private Coroutine _actionWrapperForCastCoroutine;
    private bool _isPreparing = false;
    private bool _isCasting = false;

    public StatsBuff Buff => _statsBuff;
    public string Name => _abilityInfo.Name;
    public string Description => _abilityInfo.Description;
    public Sprite Icon => _abilityInfo.Icon;
    public bool IsCooldowned { get => _remaining—ooldownTime < 0; }
    public int Chargers => _currentChargers;
    public bool IsHaveCharge => (_currentChargers > 0);
    public float ChargeCooldown => _chargeCooldown;
    public bool IsPreparing => _isPreparing;
    public bool IsHaveManaOnSkill { get => ManaCost <= _hero.Stamina.Value; }
    public bool IsHaveResurces { get => IsHaveManaOnSkill && IsCooldowned && IsHaveCharge; }
    public float ManaCost { get => Buff.ManaCost.GetBuffedValue(_manaCost); }
    public float CooldownTime { get => Buff.Cooldown.GetBuffedValue(_cooldownTime); }
    public float CastDeley { get => Buff.CastSpeed.GetBuffedValue(_castDeley); }
    public bool IsCasting { get => _isCasting; set => _isCasting = value; }
    public LayerMask TargetsLayers => _targetsLayers; 

    public event Action<int> CurrentChargeChange;
    public event Action<float> CooldownStarted;
    public event Action CooldownEnded;
    public event Action PreparingStarted;
    public event Action PreparingEnded;
    public event Action<float> CastDeleyStarted;
    public event Action CastDeleyEnded;
    public event Action CastingStarted;
    public event Action CastingEnded;
    public event Action Canceled;
    public event Action<float> MassageHaventMana;
    public event Action MassageHaventCharge;
    public event Action<float> MassageNotCooldowned;

    protected abstract bool IsCanCast { get; }

    protected abstract IEnumerator PreparingJob();
    protected abstract IEnumerator CastJob();
    protected abstract void ClearData();

    protected virtual void Awake()
    {
        if (_isUseCharges)
            _currentChargers = _maxCharges;
        else
            _currentChargers = 1;
    }

    public bool TryPreparing()
    {
        if(_isPreparing == false)
        {
            _actionWrapperForPreparingCoroutine = StartCoroutine(ActionWrapperForPreparingJob());
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TryCast()
    {
        CheckResources();

        if (IsHaveResurces && IsCanCast && _isCasting == false)
        {
            _actionWrapperForCastCoroutine = StartCoroutine(ActionWrapperForCastingJob());
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Cancel(bool foceCancel = false)
    {
        if (foceCancel || _isCanCancle)
        {
            ClearData();

            if (_castingCoroutine != null)
                StopCoroutine(_castingCoroutine);

            if(_preparingCoroutine != null)
                StopCoroutine(_preparingCoroutine);

            Canceled?.Invoke();
        }
    }

    public void SetCooldown(float time)
    {
        if (time < _remaining—ooldownTime)
            return;

        if (_cooldownJob != null)
            StopCoroutine(_cooldownJob);

        _cooldownJob = StartCoroutine(CooldownCoroutine(time));
    }

    protected virtual void PayCost()
    {
        _hero.Stamina.Use(_manaCost);
        SetCooldown(CooldownTime);

        if (_isUseCharges)
            TryUseCharge();
    }

    protected Coroutine TryStartAndGetCastDeleyCoroutine()
    {
        _castDeleyJob = StartCoroutine(CastDeleyCoroutine());
        return _castDeleyJob;
    }

    private bool TryUseCharge()
    {
        if (_isUseCharges == false)
            return true;

        if (_currentChargers > 0)
        {
            _currentChargers--;
            CurrentChargeChange?.Invoke(_currentChargers);

            if (_rechargeJob == null || _chargesHaveSeparateCooldown)
                _rechargeJob = StartCoroutine(RechargeCoroutine());
            return true;
        }
        else
        {
            return false;
        }
    }

    private void CheckResources()
    {
        if (IsHaveManaOnSkill == false)
            MassageHaventMana?.Invoke(ManaCost - _hero.Stamina.Value);

        if (IsCooldowned == false)
            MassageNotCooldowned?.Invoke(_remaining—ooldownTime);

        if (IsHaveCharge == false)
            MassageHaventCharge?.Invoke();
    }

    private IEnumerator RechargeCoroutine()
    {
        while (_currentChargers < _maxCharges)
        {
            float time = 0;
            while (time < ChargeCooldown)
            {
                time += Time.deltaTime;
                yield return null;
            }
            _currentChargers++;
            CurrentChargeChange?.Invoke(_currentChargers);
        }
        _rechargeJob = null;
    }

    private IEnumerator CooldownCoroutine(float cooldownTime)
    {
        CooldownStarted?.Invoke(cooldownTime);
        _remaining—ooldownTime = cooldownTime;

        while (_remaining—ooldownTime > 0)
        {
            _remaining—ooldownTime -= Time.deltaTime;
            yield return null;
        }
        CooldownEnded?.Invoke();
        _cooldownJob = null;
    }

    private IEnumerator CastDeleyCoroutine()
    {
        CastDeleyStarted?.Invoke(CastDeley);
        float time = 0;

        while (time < CastDeley)
        {
            time += Time.deltaTime;
            yield return null;
        }
        _castDeleyJob = null;
        CastDeleyEnded?.Invoke();
    }

    private IEnumerator ActionWrapperForPreparingJob()
    {
        PreparingStarted?.Invoke();
        _isPreparing = true;
        yield return _preparingCoroutine = StartCoroutine(PreparingJob());
        PreparingEnded?.Invoke();
        _isPreparing = false;

        _preparingCoroutine = null;
    }

    private IEnumerator ActionWrapperForCastingJob()
    {
        CastingStarted?.Invoke();
        _isCasting = true;

        if (CastDeley > 0)
            yield return TryStartAndGetCastDeleyCoroutine();

        yield return _castingCoroutine = StartCoroutine(CastJob());
        CastingEnded?.Invoke();
        _isCasting = false;

        _castingCoroutine = null;
    }
}
