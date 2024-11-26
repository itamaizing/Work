using Mirror;
using System;
using System.Collections;
using UnityEngine;

public enum ResourceType 
{
    Health, 
    Mana, 
    Energy, 
    Rune
}

public abstract class Resource : NetworkBehaviour
{
    [SerializeField] private ResourceType _resourceType;
    [SerializeField, SyncVar] protected float _regenerationDelay = 0;
    [SyncVar(hook = nameof(HookValueChanged))] protected float _currentValue;
    [SyncVar(hook = nameof(HookMaxValueChanged))] protected float _maxValue;
    [SyncVar] protected float _regenerationValue;
    [SyncVar] protected float _regenerationPeriod;
    
    
    protected Coroutine _regenCoroutine;

    public float CurrentValue { get => _currentValue; protected set { _currentValue = value; } }
    public float MaxValue { get => _maxValue; protected set { _maxValue = value; } }
    public float RegenerationValue { get => _regenerationValue;  set { _regenerationValue = value; } }
    public float RegenerationDelay { get => _regenerationPeriod;  set { _regenerationPeriod = value; } }

    public ResourceType Type => _resourceType;

    public event Action<float, float> MaxValueChanged;
    public event Action<float, float> ValueChanged;
    public event Action<float> PhantomValueShown;

	private void Awake()
	{
		ClientStartRegenirateJob();
	}

	public virtual void Initialize(float maxValue, float regenValue, float regenDelay, CharacterData data)
    {
        _currentValue = maxValue;
        _maxValue = maxValue;
        _regenerationValue = regenValue;
        _regenerationPeriod = regenDelay;

        /*if (regenValue > 0)
            ClientStartRegenirateJob();*/
    }

    public virtual void Add(float value)
    {
		if (_maxValue >= _currentValue + value)
            _currentValue += value;
        else
            _currentValue = _maxValue;
    }

    public virtual bool TryUse(float value)
    {
        if(_currentValue - value >= 0)
        {
            CurrentValue -= value;
            return true;
        }
        else
        {
            CurrentValue = 0;
            return false;
        }
    }

    public void PhantomValueShow(float value)
    {
        PhantomValueShown?.Invoke(value);
    }

	protected virtual void HookValueChanged(float oldValue, float newValue)
    {
        ValueChanged?.Invoke(oldValue, newValue);
    }

    protected virtual void HookMaxValueChanged(float oldValue, float newValue)
    {
        MaxValueChanged?.Invoke(oldValue, newValue);
    }

    public void ResetValue()
    {
        _currentValue = _maxValue;
    }

    public void ChangedMaxValue(float value)
    {
        _maxValue += value;

    }

    private IEnumerator RegenerateJob()
    {
        while (true)
        {
            if (_regenerationValue < 0) yield return null;

            if(_currentValue < _maxValue)
            {
                yield return new WaitForSeconds(_regenerationDelay);

                while (_currentValue < _maxValue)
                {
                    CmdRegen();
                    yield return new WaitForSeconds(_regenerationPeriod);
                }
            }
            yield return null;
        }
    }

    [Command]
    public void CmdUse(float value)
    {
        //Debug.Log(value + " try " + _currentValue);
        TryUse(value);
    }

    [Command]
    public void CmdAdd(float value)
    {
        Add(value);
    }

    [ClientCallback]
    protected void ClientStartRegenirateJob()
    {
        _regenCoroutine = StartCoroutine(RegenerateJob());
    }

    [Client]
    protected void ClientStopRegenerateJob()
    {
        if (_regenCoroutine != null)
        {
            StopCoroutine(RegenerateJob());
        }
    }

    [Command]
    protected void CmdRegen()
    {
        Add(_regenerationValue);
    }
}
