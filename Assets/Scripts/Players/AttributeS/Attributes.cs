using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Attributes
{
    public string Name;
    public string Description;

    [SerializeField] private float _value;
    private List<AttributeModifiers> _modifiers = new();

    public List<AttributeModifiers> Modifiers => _modifiers;

    public Attributes(string name, string description = null)
    {
        Name = name;
        Description = description;
        //_value = value; 
    }

    public void AddModifier(AttributeModifiers modifier)
    {
        _modifiers.Add(modifier);
    }

    public void RemoveModifier(AttributeModifiers modifier)
    {
        if(_modifiers.Contains(modifier))
            _modifiers.Remove(modifier);
    }

    public void SetValue(float value)
    {
        _value = value;
    }

    public float GetValue()
    {
        float value;
        float flat = 0, percent = 1, multipliy = 1, menuFlat = 0;

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
                    percent += modifier.Value;
                    break;
                case ModifierType.MenuFlat:
                    menuFlat += modifier.Value;
                    break;
                default:
                    break;
            }
        }
        value = (_value + flat + menuFlat) * percent * multipliy; 
        return value;
    }
}

[Serializable]
public struct AttributeModifiers
{
    public AttributeModifiers(float value, ModifierType type)
    {
        Value = value;
        Type = type;
    }

    public float Value;
    public ModifierType Type;
}

public enum ModifierType
{
    Flat,
    Percent,
    Multiplier,
    MenuFlat
}