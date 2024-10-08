using Mirror;
using System;

public struct Damage : NetworkMessage
{
    public float Value;
    public DamageType Type;
    public Schools School;
    public AbilityForm Form;
    public AttackRangeType PhysicAttackType;
    public Skill DamageableSkill;
}

public interface IDamageable
{
    public event Action<float, DamageType, Skill> DamageTaken;
    public bool TryTakeDamage(ref Damage damage, Skill skill);
}
