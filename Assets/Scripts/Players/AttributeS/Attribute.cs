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
    private float _flat = 0, _percent = 1, _multiplier = 1, _menuFlat = 0;

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
    public Attribute(float _value=0)
    {
        //Name = name;
        _baseValue = _value; 
    }

    public void AddModifier(AttributeModifier modifier)
    {
        _modifiers.Add(modifier);
        _isActual = false;
    }

    public void RemoveModifier(AttributeModifier modifier)
    {
        if(_modifiers.Contains(modifier))
            _modifiers.Remove(modifier);
        _isActual = false;
    }

    public void SetBaseValue(float value)
    {
        _baseValue = value;
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
                    multiplier *= (1 + modifier.Value);
                    break;
                case ModifierType.MenuFlat:
                    menuFlat += modifier.Value;
                    break;
                default:
                    break;
            }
        }
        _flat = flat;
        _percent = percent;
        _multiplier = multiplier;
        _menuFlat = menuFlat;
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
        float final = (value + _flat + _menuFlat) * (1 + _percent) * _multiplier;
        return final;
    }


    private void UpdateCached()
    {
        _cachedValue = CalculateFor(_baseValue);
        _isActual = true;
    }
}
#endregion

#region Modifier
/// <summary>
/// <param name="Type">
/// All ModifierValues should be passed as percent. I.e. 0.30 = 30% boost
/// </param>
/// </summary>
[Serializable]
public class AttributeModifier
{
    public AttributeModifier(float value, ModifierType type, object source=null)
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
/// Flat: add to base value
/// Percent: additive multiplier. 0.30 + 0.20 = *50%
/// Multiplier: multiplicative multiplier. 0.30 * 0.20 = *56%
/// </code>
/// </summary>
public enum ModifierType
{
    Flat,
    MenuFlat,
    Percent,
    Multiplier
}
#endregion