using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;
using Mirror;
using System;


public abstract class Ability : NetworkBehaviour
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
	[SerializeField] protected Schools _abilitySchool;
	[SerializeField] protected AbilityForm _abilityForm;
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

	protected StaminaComponent _mana;
	protected MoveComponent _playerMove;
	protected HealthComponent _health;
	protected bool _isUsed = false;
	protected bool _isCanCancle = true;
	protected bool _isReady = true;
    protected int _currentChargers;
	protected Coroutine _rechargeJob;
	protected Coroutine _streamingJob;
	protected Coroutine _castDeleyJob;
	protected Coroutine _cooldownJob;

    private float _remainingСooldownTime;
	private bool _avaliable;
	private float _timerForDebuf;
    private StatsBuff _statsBuff = new StatsBuff(1, 0);

	public MoveComponent PlayerMove => _playerMove;
    public StaminaComponent Mana => _mana;
    public HealthComponent Health => _health;
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
    public float CastDeley { get => Buff.CastSpeed.GetBuffedValue(_castDeley); protected set => _castDeley = value; }
    public float Radius { get => Buff.Radius.GetBuffedValue(_radius); protected set => _radius = value; }
    public float Area { get => Buff.Area.GetBuffedValue(_area); protected set => _area = value; }
    public float CastLength { get => Buff.Area.GetBuffedValue(_castLength); protected set => _castLength = value; }
    public float CastWidth { get => Buff.Area.GetBuffedValue(_castWidth); protected set => _castWidth = value; }
    public bool IsAutoAttack { get => _isAutoAttack; protected set => _isAutoAttack = value; }
    public bool IsUsed { get => _isUsed; protected set => _isUsed = value; }
    public bool IsCanCancle { get => _isCanCancle; protected set => _isCanCancle = value; }
    public bool IsReady { get => _isReady; set => _isReady = value; }
	public Schools School => _abilitySchool;
	public AbilityForm AbilityForm => _abilityForm;
    public StatsBuff Buff => _statsBuff;

    public event UnityAction<int> CurrentChargeChange;
    public event UnityAction<float> StartStreaming;
    public event UnityAction StopStreaming;
    public event UnityAction<float> StartCastDeley;
    public event UnityAction StopCastDeley;
    public event UnityAction Cancled;
    public event UnityAction CastStarted;
    public event UnityAction PreparingEnded;
    public event UnityAction CastEnded;
    public event UnityAction AreaOffed;
    public event UnityAction<float> CooldownStarted;

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

    public void SetPlayer(MoveComponent playerMove, StaminaComponent mana, HealthComponent health)
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
            Cancled?.Invoke();
            return true;
        }
        return false;
    }

    public virtual bool TryUse()
    {
        if (_isUsed || (_mana.Value >= _manaCost && _isReady) == false && !_avaliable)
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
        CastStarted?.Invoke();
        Cast();
        return true;
    }

    public void SetCooldown(float time)
    {
        _isReady = false;

        if (time < _remainingСooldownTime)
            return;

        if(_cooldownJob != null)
            StopCoroutine(_cooldownJob);

        _cooldownJob = StartCoroutine(CooldownCoroutine(time));
    }

    protected Coroutine GetCastDeleyCoroutine()
    {
        _castDeleyJob = StartCoroutine(CastDeleyCoroutine());
        StartCastDeley?.Invoke(CastDeley);
        return _castDeleyJob;
    }

    protected virtual bool PayCost(bool castEnded = true)
    {
        if (TryUseCharge() && _mana.Value >= _manaCost && _isReady)
        {
            CmdUseMana(_manaCost);
        }
        else
        {
            TryCancel();
            return false;
        }
        _isReady = false;
        _cooldownJob = StartCoroutine(CooldownCoroutine(_cooldown));
        PreparingEnded?.Invoke();

        if (_isStreaming)
        {
            if(_streamingJob != null)
            {
                StopCoroutine(_streamingJob);
                _streamingJob = null;
            }
            _streamingJob = StartCoroutine(ManaCostPerTickCorutine());
            return true;
        }
        if (castEnded)
        {
            _isUsed = false;
            CastEnded?.Invoke();
        }
        return true;
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

    protected Vector2 GetMousePoint()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    protected void ApplyDamage(HealthComponent health, float damage, DamageType damageType, AttackRangeType attackRangeType)
    {
        CmdApplyDamage(health.gameObject, Buff.Damage.GetBuffedValue(damage), damageType, attackRangeType);
    }

    private IEnumerator CooldownCoroutine(float cooldownTime)
    {
        CooldownStarted?.Invoke(cooldownTime);
        _remainingСooldownTime = cooldownTime;

        while (_remainingСooldownTime > 0)
        {
            _remainingСooldownTime -= Time.deltaTime;
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

        while (time < CastDeley)
        {
            time += Time.deltaTime;
            yield return null;
        }
        _playerMove.CanMove = true;
        _castDeleyJob = null;
        //CastEnded?.Invoke();
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
            CmdUseMana(_manaCostPerTick);
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
    
    [Command]
    protected void CmdInstantiate(GameObject gameObject)
    {
        GameObject item = Instantiate(gameObject, transform.position, Quaternion.identity);
        NetworkServer.Spawn(item);
    }

    [Command]
    protected void CmdInstantiate(GameObject gameObject, GameObject parent)
    {
        GameObject item = Instantiate(gameObject, parent.transform.parent);
        NetworkServer.Spawn(item);
    }

    [Command]
    protected void CmdUseMana(float value)
    {
        _mana.Use(value);
    }

    [Command]
    private void CmdApplyDamage(GameObject target, float damage, DamageType damageType, AttackRangeType attackRangeType)
    {
        target.GetComponent<HealthComponent>().TryTakeDamage(damage, damageType, attackRangeType);
    }

	public void SwitchAvailible(bool avalieble)
	{
		_avaliable = avalieble;
	}

	public void KnockDownTimerStart(float time)
	{
		_timerForDebuf = time;
		StartCoroutine(KnockDownTimer());
	}

	private IEnumerator KnockDownTimer()
	{
		yield return new WaitForSeconds(_timerForDebuf);
		_avaliable = true;
	}
}

public enum Schools
{
	Light,
	Dark,
	Fire,
	Water,
	Air,
	Earth,
	Physical,
    None
}

public enum AbilityForm
{
	Spell,
	Magic,
	Physical
}

public struct StatsBuff
{
    private StatBuff _damage;
    private StatBuff _radius;
    private StatBuff _area;
    private StatBuff _attackSpeed;
    private StatBuff _castSpeed;

    public StatBuff Damage => _damage;
    public StatBuff Radius => _radius;
    public StatBuff Area => _area;
    public StatBuff AttackSpeed => _attackSpeed;
    public StatBuff CastSpeed => _castSpeed;

    public StatsBuff(float multiplier, float additional)
    {
        _damage = new StatBuff(multiplier, additional);
        _radius = new StatBuff(multiplier, additional);
        _area = new StatBuff(multiplier, additional);
        _attackSpeed = new StatBuff(multiplier, additional);
        _castSpeed = new StatBuff(multiplier, additional);
    }
}

public struct StatBuff
{
    private float _multiplier;
    private float _additional;

    public float Multiplier => _multiplier;
    public float Additional => _additional;

    public StatBuff(float multiplier, float additional)
    {
        _multiplier = multiplier;
        _additional = additional;
    }

    public float GetBuffedValue(float value)
    {
        return (value + _additional) * _multiplier;
    }

    public void IncreasePercentage(float value)
    {
        _multiplier *= value;
    }

    public void ReductionPercentage(float value)
    {
        _multiplier /= value;
    }

    public void AddValue(float value)
    {
        _additional += value;
    }
}