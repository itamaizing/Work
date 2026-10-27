using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum DamageType
{
    Magical,
    Physical,
    Both,
    Inner,
    None
}

public struct Damage : NetworkMessage
{
    public float Value;
    public DamageType Type;
    public Schools School;
    public AbilityForm Form;
    public AttackRangeType AttackRange;
    public TargetingType TargetingType;
    public Skill SourceSkill;
    public string DamageKey;
    public bool FullyAbsorbed;
    public bool IsDot;
}

public interface IDamageable
{
    public event Action<Damage, Skill> DamageTaken;
    //public event Action<float> PhantomValueShown;
    public bool TryTakeDamage(ref Damage damage, Skill skill);
    public void ShowPhantomValue(Damage phantomValue);
    Transform transform { get; }
    GameObject gameObject { get; }
}
