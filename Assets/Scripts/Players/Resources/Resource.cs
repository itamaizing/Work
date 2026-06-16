using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
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
    private float _slowRegenDebt = 0f;

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
    
    protected Coroutine _regenCoroutine;
    private Coroutine _slowRegenCoroutine;
    protected Attribute _maxValueAttribute;
    protected Attribute _regenValueAttribute;

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
    
    private readonly List<AttributeModifier> _incomingModifiers = new List<AttributeModifier>();
    
    private readonly Dictionary<float, float> _regenMods = new();
    private Coroutine _regenModCoroutine;

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

        _regenValueAttribute = regenValue;
        _regenerationValue = regenValue.GetValue();


        _maxValueAttribute = maxValue;
        _maxValue = maxValue.GetValue();
        _currentValue = _maxValue;

        if (isServer) _regenCoroutine = StartCoroutine(RegenerateJob());
    }

    // Можно перевести на такой же формат хранения атрибутов (ResourceAttribute) - тогда можно вообще весь хардкод убрать
    public virtual void Init(ResourceAttribute resource) 
    {
        _attr_regenValue = resource.Attributes[ResourceAttributeName.Regen];
        _regenerationValue = resource.Attributes[ResourceAttributeName.Regen].GetValue();

        _attr_maxValue = resource.Attributes[ResourceAttributeName.MaxValue];
        _maxValue = resource.Attributes[ResourceAttributeName.MaxValue].GetValue();

        _attr_regenDelay = resource.Attributes[ResourceAttributeName.RegenDelay];
        _attr_regenPeriod = resource.Attributes[ResourceAttributeName.RegenPeriod];
        
        _currentValue = _maxValue;

        _regenCoroutine = StartCoroutine(RegenerateJob());
        ClientStartRegenirateJob();
    }
    
    public void AddIncomingModifier(AttributeModifier modifier)
    {
        _incomingModifiers.Add(modifier);
    }

    public void RemoveIncomingModifier(AttributeModifier modifier)
    {
        _incomingModifiers.Remove(modifier);
    }
    
    protected float ApplyIncomingModifiers(float baseValue)
    {
        if (_incomingModifiers.Count == 0) 
            return baseValue;

        float multiplier = 1f;
        float flatBonus = 0f;

        foreach (var mod in _incomingModifiers)
        {
            if (mod.Type == ModifierType.Flat)
                flatBonus += mod.Value;
            else if (mod.Type == ModifierType.Percent)
                multiplier += mod.Value;
            else if (mod.Type == ModifierType.Multiplier)
                multiplier *= (1f + mod.Value);
        }

        return (baseValue + flatBonus) * multiplier;
    }

    public virtual void Add(float value)
    {
        Debug.Log("Try regen " + value);
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
    
    [Command]
    public void CmdRemoveAllRegenModifiers()
    {
        RemoveAllRegenModifiers();
    }

    private void RemoveAllRegenModifiers()
    {
        if (_regenMods.Count == 0) return;

        _regenMods.Clear();
        _regenerationValue = _attr_regenValue.GetValue();

        if (_regenCoroutine != null)
        {
            StopCoroutine(_regenCoroutine);
            _regenCoroutine = StartCoroutine(RegenerateJob());
        }
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
    
    [Command(requiresAuthority = false)]
    public void CmdAddRegenModifier(float energy, float multiplier, bool isFast)
    {
        float delta = isFast ? -energy : energy;
        _regenMods.TryGetValue(multiplier, out float current);
        float newVal = current + delta;

        if (Mathf.Approximately(newVal, 0f))
            _regenMods.Remove(multiplier);
        else
            _regenMods[multiplier] = newVal;

        if (_regenModCoroutine == null && _regenMods.Count > 0)
            _regenModCoroutine = StartCoroutine(ProcessRegenMods());
    }
    
    [Command(requiresAuthority = false)]
    public void CmdAddRegenModifierByTime(float seconds, float multiplier, bool isFast)
    {
        float regenPerSecond = _regenerationValue / _regenerationPeriod;
        float energy = regenPerSecond * seconds * (isFast ? multiplier : 1f / multiplier);
        CmdAddRegenModifier(energy, multiplier, isFast);
    }
    
    private IEnumerator ProcessRegenMods()
    {
        float savedRegen = _regenerationValue;

        while (_regenMods.Count > 0)
        {
            float mult = 0f, net = 0f;
            foreach (var kv in _regenMods) { mult = kv.Key; net = kv.Value; break; }

            _regenerationValue = net > 0
                ? savedRegen / mult
                : savedRegen * mult;

            if (_regenCoroutine != null)
            {
                StopCoroutine(_regenCoroutine);
                _regenCoroutine = StartCoroutine(RegenerateJob());
            }

            while (_regenMods.TryGetValue(mult, out float remaining)
                   && !Mathf.Approximately(remaining, 0f))
            {
                if (_regenerationValue <= 0f) { _regenMods.Remove(mult); break; }

                yield return new WaitForSeconds(_regenerationPeriod);

                if (_currentValue < _maxValue)
                {
                    float regened = _regenerationValue;

                    float updated = remaining > 0
                        ? remaining - regened
                        : remaining + regened;

                    if (Mathf.Approximately(updated, 0f) || (remaining > 0 && updated <= 0) || (remaining < 0 && updated >= 0))
                    {
                        _regenMods.Remove(mult);
                        break;
                    }
                    else
                    {
                        _regenMods[mult] = updated;
                    }
                }
            }
        }

        _regenerationValue = savedRegen;

        if (_regenCoroutine != null)
        {
            StopCoroutine(_regenCoroutine);
            _regenCoroutine = StartCoroutine(RegenerateJob());
        }

        _regenModCoroutine = null;
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
