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
    [SerializeField] protected bool _isAutoAttack;
    [SerializeField] protected float _radius = 0f;
    [SerializeField] protected float _area = 0f;
    [SerializeField] protected float _castLength = 0f;
    [SerializeField] protected float _castWidth = 0f;
    [SerializeField] protected float _manaCost = 0f;
    [SerializeField] protected float _castDeley = 0f;
    [SerializeField] protected float _cooldown = 0f;
    [Header("Charge settings")]
    [SerializeField] protected bool _isUseCharges;
    [SerializeField] protected bool _chargesHaveSeparateCooldown;
    [SerializeField] protected int _maxCharges;
    [SerializeField] protected float _chargeCooldown;
    [Header("Streaming settings")]
    [SerializeField] protected bool _isStreaming;
    [SerializeField] protected float _streamingDuration;
    [SerializeField] protected float _manaCostRate;
    [SerializeField] protected float _manaCostPerTick;

    protected Character _character;
    protected PlayerStamina _mana;
    protected PlayerMove _playerMove;
    protected HealthPlayer _health;
    protected bool _isUsed = false;
    protected bool _isCanCancle = true;
    protected bool _isReady = true;
    protected int _currentChargers;
    protected Coroutine _rechargeJob;
    protected Coroutine _streamingJob;
    protected Coroutine _castDeleyJob;
    protected Coroutine _cooldownJob;

    public PlayerMove PlayerMove => _playerMove;
    public PlayerStamina Mana => _mana;
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
    public float CastLength { get => _castLength; protected set => _castLength = value; }
    public float CastWidth { get => _castWidth; protected set => _castWidth = value; }
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
    public event UnityAction PreparingEnded;
    public event UnityAction CastEnded;
    public event UnityAction AreaOffed;

    protected abstract void Cast();
    protected abstract void Cancel();

    protected virtual void Start()
    {
        if (_isUseCharges)
        {
            _currentChargers = _maxCharges;
            CurrentChargeChange?.Invoke(_currentChargers);
        }
    }

    public void Init(Character character)
    {
        _playerMove = character.Move;
        _mana = character.Stamina;
        _health = character.Health;
        _character = character;
    }

    public virtual bool TryCancel()
    {
        if(_isUsed && _isCanCancle)
        {
            Cancel();
            _isUsed = false;
            _playerMove.CanMove = true;

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
        if (_isUsed && (_mana.Value >= _manaCost && _isReady) == false)
        {
            PreparingEnded?.Invoke();
            return false;
        }
        if (_isUseCharges)
        {
            if (IsHaveCharge == false)
            {
                PreparingEnded?.Invoke();
                return false;
            }    
        }
        _isUsed = true;
        _isCanCancle = true;
        Cast();
        return true;
    }

    protected virtual void PayCost()
    {
        if (TryUseCharge() && _mana.Value >= _manaCost && _isReady)
        {
            _mana.Use(_manaCost);
        }
        else
        {
            TryCancel();
            return;
        }
        _isReady = false;
        _cooldownJob = StartCoroutine(CooldownCoroutine());
        PreparingEnded?.Invoke();

        if (_isStreaming)
        {
            if(_streamingJob != null)
            {
                StopCoroutine(_streamingJob);
                _streamingJob = null;
            }
            _streamingJob = StartCoroutine(ManaCostPerTickCorutine());
            return;
        }
        CastEnded?.Invoke();
        _isUsed = false;
    }

    protected Coroutine GetCastDeleyCoroutine()
    {
        _castDeleyJob = StartCoroutine(CastDeleyCoroutine());
        StartCastDeley?.Invoke(_castDeley);
        return _castDeleyJob;
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

    protected void AreaOff()
    {
        AreaOffed?.Invoke();
    }

    protected bool IsMouseInRadius(float radius)
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= radius;
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
        _playerMove.CanMove = false;

        PreparingEnded?.Invoke();
        float time = 0;

        while (time < _castDeley)
        {
            time += Time.deltaTime;
            yield return null;
        }
        _playerMove.CanMove = true;
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
        _playerMove.CanMove = false;
        StartStreaming?.Invoke(_streamingDuration);
        float time = 0;

        while (time < _streamingDuration + _manaCostRate && _mana.Value >= _manaCostPerTick)
        {
            Mana.Use(_manaCostPerTick);
            time += _manaCostRate;
            yield return new WaitForSeconds(_manaCostRate);
        }
        StopStreaming?.Invoke();
        _playerMove.CanMove = true;
        _isCanCancle = true;
        TryCancel();
        CastEnded?.Invoke();
        _streamingJob = null;
    }
}
