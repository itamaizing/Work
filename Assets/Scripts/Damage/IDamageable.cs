using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Damage : NetworkMessage
{
    public float Value;
    public DamageType Type;
    public Schools School;
    public AbilityForm Form;
    public AttackRangeType PhysicAttackType;
}

public interface IDamageable
{
    public event Action<float, DamageType, Skill> DamageTaken;
	//public event Action<float> PhantomValueShown;
	public bool TryTakeDamage(ref Damage damage, Skill skill);
    public void ShowPhantomValue(Damage phantomValue);

}
