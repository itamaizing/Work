using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#region Attribute
[Serializable]
public class Attribute
{
    //public BasicAttributeName Name;

    [SerializeField] private float _baseValue;
    [SerializeField] private float _cachedValue;
    private bool isActual = false;
    [SerializeField] private List<AttributeModifier> _modifiers = new();
    

    //public Attribute(BasicAttributeName name, float _value=0)
    public Attribute(float _value=0)
    {
        //Name = name;
        _baseValue = _value; 
    }

    public void AddModifier(AttributeModifier modifier)
    {
        _modifiers.Add(modifier);
        isActual = false;
    }

    public void RemoveModifier(AttributeModifier modifier)
    {
        if(_modifiers.Contains(modifier))
            _modifiers.Remove(modifier);
        isActual = false;
    }

    public void SetBaseValue(float value)
    {
        _baseValue = value;
    }

    public float Recalculate()
    {
        float value;
        float flat = 0, percent = 0, multiplier = 1;

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
                default:
                    break;
            }
        }
        value = (_baseValue + flat) * (1 + percent) * multiplier;
        _cachedValue = value;
        isActual = true;
        return value;
    }

    public float GetValue()
    {
        if (isActual)
            return _cachedValue;
        return Recalculate();
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
    Percent,
    Multiplier
}
#endregion
