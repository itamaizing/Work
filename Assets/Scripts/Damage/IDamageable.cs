using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Damage : NetworkMessage
{
    public float Value;
    public DamageType Type;
    public AttackRangeType Range;
}

public interface IDamageable
{
    public event Action<float, DamageType> DamageTaken;
    public bool TryTakeDamage(ref Damage damage, Skill skill);
}
