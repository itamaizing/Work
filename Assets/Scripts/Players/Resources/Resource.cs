using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public abstract class Resource : NetworkBehaviour
{
    [SyncVar(hook = nameof(HookValueChanged))] protected float _currentValue;
    [SyncVar(hook = nameof(HookMaxValueChanged))] protected float _maxValue;
    [SyncVar] protected float _regenerationValue;
    [SyncVar] protected float _regenerationDelay;
    protected Coroutine _regenCoroutine;

    public float CurrentValue { get => _currentValue; protected set { _currentValue = value; } }
    public float MaxValue { get => _maxValue; protected set { _maxValue = value; } }
    public float RegenerationValue { get => _regenerationValue;  set { _regenerationValue = value; } }
    public float RegenerationDelay { get => _regenerationDelay;  set { _regenerationDelay = value; } }

    public event Action<float, float> MaxValueChanged;
    public event Action<float, float> ValueChanged;

    public virtual void Initialize(float maxValue, float regenValue, float regenDelay)
    {
        _currentValue = maxValue;
        _maxValue = maxValue;
        _regenerationValue = regenValue;
        _regenerationDelay = regenDelay;

        if (regenValue > 0)
            ClientStartRegenirateJob();
    }

    public virtual void Add(float value)
    {
        if (MaxValue >= _currentValue + value)
            CurrentValue += value;
        else
            CurrentValue = _maxValue;
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

    protected virtual void HookValueChanged(float oldValue, float newValue)
    {
        ValueChanged?.Invoke(oldValue, newValue);
    }

    protected virtual void HookMaxValueChanged(float oldValue, float newValue)
    {
        MaxValueChanged?.Invoke(oldValue, newValue);
    }

    private IEnumerator RegenirateJob()
    {
        while (true)
        {
            CmdRegen();
            yield return new WaitForSeconds(_regenerationDelay);
        }
    }

    [Command]
    public void CmdUse(float value)
    {
        TryUse(value);
    }

    [ClientCallback]
    protected void ClientStartRegenirateJob()
    {
        _regenCoroutine = StartCoroutine(RegenirateJob());
    }

    [Client]
    protected void ClientStopRegenirateJob()
    {
        if (_regenCoroutine != null)
        {
            StopCoroutine(RegenirateJob());
        }
    }

    [Command]
    protected void CmdRegen()
    {
        Add(_regenerationValue);
    }
}
