using Mirror;
using System;
using System.Collections;
using UnityEngine;

public enum ResourceType
{
    Health,
    Mana,
    Energy,
    Rune,
    Psionic,
    CooldownEnergy,
}

public abstract class Resource : NetworkBehaviour, IAttribute
{
    [SerializeField] private ResourceType _resourceType;
    [SerializeField, SyncVar] protected float _regenerationDelay = 0;
    [SyncVar(hook = nameof(HookValueChanged)), SerializeField] protected float _currentValue;
    [SyncVar(hook = nameof(HookMaxValueChanged)), SerializeField] protected float _maxValue;
    [SyncVar] protected float _regenerationValue;
    [SyncVar] protected float _regenerationPeriod;

    protected Coroutine _regenCoroutine;

    #region Attributes
    protected Attribute _attr_maxValue;
    protected Attribute _attr_regenValue;
    protected Attribute _attr_regenPeriod;
    protected Attribute _attr_regenDelay;

    public Attribute Attr_MaxValue => _attr_maxValue;
    public Attribute Attr_RegenValue => _attr_regenValue;
    public Attribute Attr_RegenPeriod => _attr_regenPeriod;
    public Attribute Attr_RegenDelay => _attr_regenDelay;
    #endregion

    public float CurrentValue { get => _currentValue; set { ValueChanged?.Invoke(_currentValue, value); _currentValue = value; } }
    public float MaxValue
    {
        get => _maxValue;
        private set
        {
            MaxValueChanged?.Invoke(_maxValue, value);
            _maxValue = value;
        }
    }

    public float RegenerationValue {
        get => _attr_regenValue.GetValue();
        set {
            _attr_regenValue.SetBaseValue(value);
        }
    }
    public float RegenerationPeriod {
        get => _attr_regenPeriod.GetValue();
        set {
            _attr_regenPeriod.SetBaseValue(value);
        }
    }

    public ResourceType Type => _resourceType;

    public event Action<float, float> MaxValueChanged;
    public event Action<float, float> ValueChanged;
    public event Action<float> PhantomValueShown;
    public event Action<Color> ChangedBarColor;

    private void Awake()
    {
        //ClientStartRegenirateJob();
    }

    private void OnEnable()
    {
        //ClientStartRegenirateJob();
    }

    public void CharacterInitialized()
    {
        ClientStartRegenirateJob();
    }


    private void OnDisable()
    {
        ClientStopRegenerateJob();
    }

    /*  public virtual void Initialize(float maxValue, float regenValue, float regenDelay, CharacterData data, Attribute attribute)
      {
          _currentValue = maxValue / 2;
          _maxValue = maxValue;
          _regenerationValue = regenValue;
          _regenerationPeriod = regenDelay;


          _maxValueAttribute = attribute;
          _maxValue = attribute.GetValue();
          _currentValue = _maxValue / 2;
          /*if (regenValue > 0)
              ClientStartRegenirateJob();
      }*/

    public virtual void Initialize(Attribute maxValue, Attribute regenValue, CharacterData data)
    {
        //Debug.Log("Init resourse " + maxValue.GetValue());

        _attr_regenValue = regenValue;
        _regenerationValue = _attr_regenValue.GetValue();

        _attr_maxValue = maxValue;
        MaxValue = _attr_maxValue.GetValue();
        _attr_maxValue.OnAttributeModify += OnMaxAttributeChange;

        _attr_regenDelay = new(ResourceAttributeName.RegenDelay.ToString(), 0.5f);
        _attr_regenPeriod = new(ResourceAttributeName.RegenPeriod.ToString(), 0.5f);
        
        CurrentValue = _maxValue;

        if (isServer) _regenCoroutine = StartCoroutine(RegenerateJob());
    }

    // Можно перевести на такой же формат хранения атрибутов (ResourceAttribute) - тогда можно вообще весь хардкод убрать
    public virtual void Init(ResourceAttribute resource)
    {
        _attr_regenValue = resource.Attributes[ResourceAttributeName.Regen];
        _regenerationValue = _attr_regenValue.GetValue();

        _attr_maxValue = resource.Attributes[ResourceAttributeName.MaxValue];
        MaxValue = _attr_maxValue.GetValue();
        _attr_maxValue.OnAttributeModify += OnMaxAttributeChange;

        _attr_regenDelay = resource.Attributes[ResourceAttributeName.RegenDelay];
        _attr_regenPeriod = resource.Attributes[ResourceAttributeName.RegenPeriod];

        CurrentValue = _maxValue;
        _regenCoroutine = StartCoroutine(RegenerateJob());
        ClientStartRegenirateJob();
    }

    public void OnMaxAttributeChange(string name, float value)
    {
        //Debug.Log("OnMaxChanged: " + value);
        MaxValue = value;
        if (CurrentValue > MaxValue)
            CurrentValue = MaxValue;
    }

    public virtual void Add(float value)
    {
        //Debug.Log($"Try regen {value}, period{_attr_regenPeriod.GetValue()}" );

        if (_maxValue >= _currentValue + value)
            _currentValue += value;
        else
            _currentValue = _maxValue;
    }

    public virtual bool TryUse(float value)
    {
        ClientStopRegenerateJob();
        ClientStartRegenirateJob();
        if (_regenCoroutine != null)
        {
            CmdResetRegen();
            //Debug.Log("Restart regen");
            StopCoroutine(_regenCoroutine);
            _regenCoroutine = StartCoroutine(RegenerateJob());
        }
        Debug.Log($"Used {value}, now {_currentValue}");
        if (_currentValue - value >= 0)
        {
            _currentValue -= value;
            return true;
        }
        else
        {
            _currentValue = 0;
            return false;
        }
    }

    public void PhantomValueShow(float value)
    {
        //Debug.Log("SHOW PHANTOM " + gameObject + " Count " + value, this);
        PhantomValueShown?.Invoke(value);
    }

    public void InstCurrentValue(float value)
    {
        _currentValue = value;

        if (isServer) RpcResetValueUpdate();
        else HookValueChanged(0, _currentValue);
    }

    public void ChangeBarColor(Color color)
    {
        ChangedBarColor?.Invoke(color);
    }

    public void AddMax(float delta, bool keepPercent = false)
    {
        bool pauseRegen = _regenCoroutine != null && delta < 0 && Mathf.Approximately(_currentValue, _maxValue);

        if (pauseRegen)
        {
            StopCoroutine(_regenCoroutine);
            _regenCoroutine = null;
        }

        float oldMax = _maxValue;
        float oldCurrent = _currentValue;

        _maxValue += delta;

        if (delta > 0f) _currentValue += keepPercent ? _maxValue * (oldCurrent / oldMax) - oldCurrent : delta;

        else
        {
            if (keepPercent) _currentValue = _maxValue * (oldCurrent / oldMax);
            if (_currentValue > _maxValue) _currentValue = _maxValue;
        }


        if (!Mathf.Approximately(oldMax, _maxValue)) HookMaxValueChanged(oldMax, _maxValue);
        if (!Mathf.Approximately(oldCurrent, _currentValue)) HookValueChanged(oldCurrent, _currentValue);

        if (pauseRegen && _currentValue < _maxValue) _regenCoroutine = StartCoroutine(RegenerateJob());
    }


    protected virtual void HookValueChanged(float oldValue, float newValue)
    {
        ValueChanged?.Invoke(oldValue, newValue);

        //ClientStopRegenerateJob();
        //ClientStartRegenirateJob();
        if (oldValue > newValue) ResetRegen();
    }

    protected virtual void HookMaxValueChanged(float oldValue, float newValue)
    {
        MaxValueChanged?.Invoke(oldValue, newValue);
    }

    public void ResetValue()
    {
        _currentValue = _maxValue;
        RpcResetValueUpdate();
    }

    /* public void ChangedMaxValue(float value)
     {
         _maxValue += value;
     }*/

    public void Regenerate() => _regenCoroutine = StartCoroutine(RegenerateJob());

    protected IEnumerator RegenerateJob()
    {
        while (true)
        {
            if (!isServer)
            {
                yield return null;
                continue;
            }

            if (_attr_regenValue.GetValue() <= 0)
            {
                yield return null;
                continue;
            }

            if (_currentValue < _maxValue)
            {
                yield return new WaitForSeconds(_regenerationDelay);

                while (_currentValue < _maxValue)
                {
                    Add(_attr_regenValue.GetValue());
                    yield return new WaitForSeconds(RegenerationPeriod);
                }
            }

            yield return null;
        }
    }

    [Command]
    public void CmdAddMax(float delta)
    {
        AddMax(delta);
    }

    [Command]
    public void ReduceRegenerationPeriod(float value)
    {
        _regenerationPeriod *= value;
    }

    [Command]
    public void IncreaseRegenerationPeriod(float value)
    {
        _regenerationPeriod /= value;
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
        if (_regenCoroutine == null)
            _regenCoroutine = StartCoroutine(RegenerateJob());
    }

    [Client]
    protected void ClientStopRegenerateJob()
    {
        if (_regenCoroutine != null)
        {
            StopCoroutine(_regenCoroutine);
        }
    }

    [Command]
    protected void CmdRegen()
    {

        Add(_attr_regenValue.GetValue());
    }

    [ClientRpc]
    private void RpcResetValueUpdate()
    {
        HookValueChanged(0, _currentValue);
    }

    protected void ResetRegen()
    {
        //Debug.Log(_regenCoroutine);
        if (_regenCoroutine != null)
        {
            //Debug.Log("Restart regen");
            StopCoroutine(_regenCoroutine);
            _regenCoroutine = StartCoroutine(RegenerateJob());
        }
    }

    [ClientRpc]
    protected void CmdResetRegen()
    {
        ResetRegen();
    }

    public void AddModifier(AttributeModifier modif)
    {
        _attr_maxValue.AddModifier(modif);

        _maxValue = _attr_maxValue.GetValue();
    }

    public void RemoveModifier(AttributeModifier modif)
    {
        _attr_maxValue.RemoveModifier(modif);

        _maxValue = _attr_maxValue.GetValue();
    }

    /*  Вроде если повесить модификатор напрямую на атрибут - все нормально работает по сети
        Но если будут косяки - можно тут ставить значения для [syncvar] переменных (delay, period)
    */
    #region Potentially Useful
    public void AddModifier(ResourceAttributeName _attr, AttributeModifier _modif)
    {
        Attribute attr = _attr_maxValue;
        switch (_attr)
        {
            case ResourceAttributeName.MaxValue:
                attr = _attr_maxValue;
                break;

            case ResourceAttributeName.Regen:
                attr = _attr_regenValue;
                break;

            case ResourceAttributeName.RegenPeriod:
                attr = _attr_regenPeriod;
                break;

            case ResourceAttributeName.RegenDelay:
                attr = _attr_regenDelay;
                break;
        }

        attr.AddModifier(_modif);
    }

    public void RemoveModifier(ResourceAttributeName _attr, AttributeModifier _modif)
    {
        Attribute attr = _attr_maxValue;
        switch (_attr)
        {
            case ResourceAttributeName.MaxValue:
                attr = _attr_maxValue;
                break;

            case ResourceAttributeName.Regen:
                attr = _attr_regenValue;
                break;

            case ResourceAttributeName.RegenPeriod:
                attr = _attr_regenPeriod;
                break;

            case ResourceAttributeName.RegenDelay:
                attr = _attr_regenDelay;
                break;
        }

        attr.RemoveModifier(_modif);
    }

    public void RemoveModifierBySource(ResourceAttributeName _attr, object source, bool all=true)
    {
        Attribute attr = _attr_maxValue;
        switch (_attr)
        {
            case ResourceAttributeName.MaxValue:
                attr = _attr_maxValue;
                break;

            case ResourceAttributeName.Regen:
                attr = _attr_regenValue;
                break;

            case ResourceAttributeName.RegenPeriod:
                attr = _attr_regenPeriod;
                break;

            case ResourceAttributeName.RegenDelay:
                attr = _attr_regenDelay;
                break;
        }

        attr.RemoveBySource(source, all);
    }
    #endregion
}