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

    public float CurrentValue { get => _currentValue; protected set { _currentValue = value; ValueChanged?.Invoke(_currentValue); } }
    public float MaxValue { get => _maxValue; protected set { _maxValue = value; MaxValueChanged?.Invoke(_maxValue); } }

    public event Action<float> MaxValueChanged;
    public event Action<float> ValueChanged;

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
        ValueChanged?.Invoke(newValue);
    }

    protected virtual void HookMaxValueChanged(float oldValue, float newValue)
    {
        MaxValueChanged?.Invoke(newValue);
    }

    [Client]
    private void ClientStartRegenirateJob()
    {
        _regenCoroutine = StartCoroutine(RegenirateJob());
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
    protected void CmdRegen()
    {
        Add(_regenerationValue);
    }
}
