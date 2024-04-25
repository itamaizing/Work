using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

public abstract class Ability : MonoBehaviour
{
    [Header("AbilitieInfo")]
    [SerializeField] private AbilityInfo _abilityInfo;
    [Header("Settings")]
    [SerializeField] private bool _isAutoAttack;
    [SerializeField] private float _radius = 0f;
    [SerializeField] private float _area = 0f;
    [SerializeField] private float _manaCost = 0f;
    [SerializeField] private float _castDeley = 0f;
    [SerializeField] private float _cooldown = 0f;
    [Header("Charge settings")]
    [SerializeField] private bool _isUseCharges;
    [SerializeField] private bool _chargesHaveSeparateCooldown;
    [SerializeField] private int _maxCharges;
    [SerializeField] private float _chargeCooldown;
    [Header("Streaming settings")]
    [SerializeField] private bool _isStreaming;
    [SerializeField] private float _streamingDuration;
    [SerializeField] private float _manaCostRate;
    [SerializeField] private float _manaCostPerTick;

    private ManaPlayer _mana;
    private PlayerMove _playerMove;
    private HealthPlayer _health;
    private bool _isUsed = false;
    private bool _isCanCancle = true;
    private bool _isReady = true;
    private int _currentChargers;
    private Coroutine _rechargeJob;
    private Coroutine _streamingJob;
    private Coroutine _castDeleyJob;
    private Coroutine _cooldownJob;

    public PlayerMove PlayerMove => _playerMove;
    public ManaPlayer Mana => _mana;
    public HealthPlayer Health => _health;
    public string Name => _abilityInfo.Name;
    public string Description => _abilityInfo.Description;
    public Sprite Icon => _abilityInfo.Icon;
    public int Chargers => _currentChargers;
    public bool IsHaveCharge => (_currentChargers > 0);
    public float ChargeCooldown => _chargeCooldown;
    public bool IsUseCharges => _isUseCharges;
    public bool IsRechargedInTurn => _chargesHaveSeparateCooldown;
    public bool IsStreaming => _isStreaming;
    public float StreamingDuration => _streamingDuration;
    public float CastDeley { get => _castDeley; protected set => _castDeley = value; }
    public float Radius { get => _radius; protected set => _radius = value; }
    public float Area { get => _area; protected set => _area = value; }
    public bool IsAutoAttack { get => _isAutoAttack; protected set => _isAutoAttack = value; }
    public bool IsUsed { get => _isUsed; protected set => _isUsed = value; }
    public bool IsCanCancle { get => _isCanCancle; protected set => _isCanCancle = value; }
    public bool IsReady { get => _isReady; set => _isReady = value; }


    public event UnityAction<int> CurrentChargeChange;
    public event UnityAction<float> StartStreaming;
    public event UnityAction StopStreaming;
    public event UnityAction<float> StartCastDeley;
    public event UnityAction StopCastDeley;
    public event UnityAction<Ability> Cancled;

    protected abstract void Cast();
    protected abstract void Cancel();

    protected virtual void Start()
    {
        if (_isUseCharges)
        {
            _currentChargers = _maxCharges;
        }
    }

    public void SetPlayer(PlayerMove playerMove, ManaPlayer mana, HealthPlayer health)
    {
        _playerMove = playerMove;
        _mana = mana;
        _health = health;
    }

    public virtual bool TryCancel()
    {
        if(_isUsed && _isCanCancle)
        {
            Cancel();
            _isUsed = false;

            if (_streamingJob != null)
            {
                StopCoroutine(_streamingJob);
                StopStreaming?.Invoke();
            } 
            if (_castDeleyJob != null)
            {
                StopCoroutine(_castDeleyJob);
                StopCastDeley?.Invoke();
            }
            Cancled?.Invoke(this);
            return true;
        }
        return false;
    }

    public virtual bool TryUse()
    {
        if (_isUsed)
            return false;

        if (_isUseCharges)
        {
            if (IsHaveCharge == false)
                return false;
        }
        _isUsed = true;
        _isCanCancle = true;
        Cast();
        return true;
    }

    protected Coroutine GetCastDeleyCoroutine()
    {
        _castDeleyJob = StartCoroutine(CastDeleyCoroutine());
        StartCastDeley?.Invoke(_castDeley);
        return _castDeleyJob;
    }

    protected virtual void PayCost()
    {
        if (TryUseCharge() && _mana.Mana >= _manaCost && _isReady)
        {
            _mana.UseMana(_manaCost);
        }
        else
        {
            TryCancel();
            return;
        }
        _isReady = false;
        _cooldownJob = StartCoroutine(CooldownCoroutine());

        if (_isStreaming)
        {
            if(_streamingJob != null)
            {
                StopCoroutine(_streamingJob);
                _streamingJob = null;
            }
            _streamingJob = StartCoroutine(ManaCostPerTickCorutine());
            StartStreaming?.Invoke(_streamingDuration);
            return;
        }
        _isUsed = false;
    }

    protected bool TryUseCharge()
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

    private IEnumerator CooldownCoroutine()
    {
        float time = 0;
        while (time < _cooldown)
        {
            time += Time.deltaTime;
            yield return null;
        }
        _isReady = true;
        _cooldownJob = null;
    }

    private IEnumerator CastDeleyCoroutine()
    {
        float time = 0;
        while (time < _castDeley)
        {
            time += Time.deltaTime;
            yield return null;
        }
        _castDeleyJob = null;
    }

    private IEnumerator RechargeCoroutine()
    {
        while (_currentChargers < _maxCharges)
        {
            float time = 0;
            while (time < _chargeCooldown)
            {
                time += Time.deltaTime;
                yield return null;
            }
            _currentChargers++;
            CurrentChargeChange?.Invoke(_currentChargers);
        }
        _rechargeJob = null;
    }

    private IEnumerator ManaCostPerTickCorutine()
    {
        float time = 0;
        while (time < _streamingDuration + _manaCostRate)
        {
            Mana.UseMana(_manaCostPerTick);
            time += _manaCostRate;
            yield return new WaitForSeconds(_manaCostRate);
        }
        _isCanCancle = true;
        TryCancel();
        _streamingJob = null;
    }
}
