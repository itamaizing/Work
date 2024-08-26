using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Damage
{
    public float Value { get; set; }
    public DamageType Type { get; }
    public AttackRangeType Range { get; }

    public Damage(float value, DamageType type, AttackRangeType range)
    {
        Value = value;
        Type = type;
        Range = range;
    }
}

public interface IDamageable
{
    public event Action<float, DamageType> DamageTaked;
    public bool TryTakeDamage(ref Damage damage, Skill skill);
}
