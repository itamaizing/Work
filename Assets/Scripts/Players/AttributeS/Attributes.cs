using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Attributes
{
    public string Name;

    [SerializeField]private float _value;
    private List<AttributeModifiers> _modifiers;
    

    public Attributes(string name)
    {
        Name = name;
        //_value = value; 
    }

    public void AddModifier(AttributeModifiers modifier)
    {
        _modifiers.Add(modifier);
    }

    public void RemoveModifier(AttributeModifiers modifier)
    {
        _modifiers.Remove(modifier);
    }

    public float GetValue()
    {
        float value;
        float flat = 0, percent = 0, multipliy = 0;

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
                default:
                    break;
            }
        }
        value = (_value + flat) * percent * multipliy; 
        return value;
    }
}

public struct AttributeModifiers
{
    public float Value;
    public ModifierType Type;
}

public enum ModifierType
{
    Flat,
    Percent,
    Multiplier
}