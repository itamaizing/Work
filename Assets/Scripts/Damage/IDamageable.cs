using Mirror;
using System;

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
