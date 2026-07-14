using System;
using System.Collections.Generic;
using UnityEngine;

#region Attribute
[Serializable]
public class Attribute
{
    private string _name;

    [SerializeField] private float _baseValue;
    [SerializeField] private float _cachedValue;
    [SerializeField] private List<AttributeModifier> _modifiers = new();

    private bool _isActual = false;
    private float _flat = 0, _percent = 1, _multiplier = 1;

    public event Action<string, float> OnAttributeModify;

    #region Properties
    public string Name => _name;
    public List<AttributeModifier> Modifiers => _modifiers;
    public float BaseValue => _baseValue;
    public float FlatBonus
    {
        get
        {
            if (_isActual)
                return _flat;
            UpdateCached();
            return _flat;
        }
    }
    public float PercentBonus
    {
        get
        {
            if (_isActual)
                return _percent;
            UpdateCached();
            return _percent;
        }
    }
    public float MultiplierBonus
    {
        get
        {
            if (_isActual)
                return _multiplier;
            UpdateCached();
            return _multiplier;
        }
    }
    #endregion Properties
    //public Attribute(CharacterAttributeName name, float _value=0)
    public Attribute(string name, float _value = 0)
    {
        _name = name;
        _baseValue = _value;
        _isActual = false;
    }

    public static implicit operator float(Attribute attribute)
    {
        return attribute.GetValue();
    }

    public void AddModifier(AttributeModifier modifier)
    {
        _modifiers.Add(modifier);
        _isActual = false;
        UpdateCached(); // otherwise it would invoke event only when attribute is called directly
    }

    public void RemoveModifier(AttributeModifier modifier)
    {
        if (_modifiers.Contains(modifier))
            _modifiers.Remove(modifier);
        _isActual = false;
        UpdateCached();
    }

    public void RemoveBySource(object source, bool all = true)
    {
        //if(_modifiers.Contains(modifier))
        //    _modifiers.Remove(modifier);
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            if (_modifiers[i].Source == source)
            {
                _modifiers.RemoveAt(i);
                if (all == false)
                    break;
            }
        }
        _isActual = false;
        UpdateCached();
    }

    public void SetBaseValue(float value)
    {
        _baseValue = value;
        _isActual = false;
        UpdateCached();
    }

    public void RecalculateMultipliers()
    {
        float flat = 0, percent = 0, multiplier = 1, menuFlat = 0;

        foreach (var modifier in _modifiers)
        {
            switch (modifier.Type)
            {
                case ModifierType.Flat:
                    flat += modifier.Value;
                    break;
                case ModifierType.Percent:
                    percent += modifier.Value;
                    break;
                case ModifierType.Multiplier:
                    if (modifier.Value < 0 && multiplier > 0)
                        multiplier *= -1;
                    multiplier *= Mathf.Abs(modifier.Value);
                    break;
                default:
                    break;
            }
        }
        _flat = flat;
        _percent = percent;
        _multiplier = multiplier;
    }

    public float GetValue()
    {
        if (_isActual)
            return _cachedValue;
        UpdateCached();
        return _cachedValue;
    }

    public float CalculateFor(float value)
    {
        RecalculateMultipliers();
        float final = (value + _flat) * (1 + _percent) * _multiplier;
        return final;
    }

    private void UpdateCached()
    {
        _cachedValue = CalculateFor(_baseValue);
        OnAttributeModify?.Invoke(_name, _cachedValue);
        //Debug.Log($"{_name}: {_cachedValue}");
        _isActual = true;
    }
}
#endregion

#region Modifier

[Serializable]
public class AttributeModifier
{
    /// <summary>
    /// <param name="value">
    /// Positive => Increase;
    /// Negative => Decrease;
    /// </param>
    /// <param name="Type">
    /// All ModifierValues should be passed as percent. I.e. 0.30 = 30% boost
    /// </param>
    /// </summary>
    public AttributeModifier(float value, ModifierType type, object source = null)
    {
        Value = value;
        Type = type;
        Source = source;
    }

    public float Value;
    public ModifierType Type;
    public object Source;
}

/// <summary>
/// <code>
/// - Flat: add to base value
/// - Percent: additive modifier. 0.30 + 0.20 = +50%
/// - Multiplier: multiplicative modifier. 1.30 * 1.20 = +56%;
///   &gt;1 - increase 
///      Increase 2 TIMES: "2f"; 3 TIMES: "3f"
///      Increase by 20%: "1.2f"; by 170%: "2.7f"
///   &lt;1 - decrease
///      Decrease 2 TIMES: "1/2=0.5f"; 3 TIMES: "1/3=0.33f";
///      Decrease by 80%: "1-0.8=0.2f"; By 30%: "1-0.3=0.7f"
///    ANY negative multipler negates WHOLE thing (regen -> damage, positive->negative).
///    Use carefully only for very niche use-cases. 
/// </code>
/// </summary>
public enum ModifierType
{
    Flat,
    Percent,
    Multiplier
}
#endregion