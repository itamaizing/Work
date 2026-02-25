using System;
using System.Collections.Generic;
using UnityEngine;

#region InfoEnums
public enum Schools
{
    Light,
    Dark,
    Fire,
    Water,
    Air,
    Earth,
    Physical,
    Discipline,
    None
}

public enum AbilityForm
{
    Spell,
    Magic,
    Physical,
    Both,
    Passiv,
}

public enum SkillType
{
    Target,
    Projectile,
    Zone,
    NonTarget,
    NonTargetWithClick,
}

public enum Moving
{
    Static,
    NonStatic
}

public enum AutoAttack
{
    autoAttack,
    nonAutoAttack
}

public enum DamageType
{
    Magical,
    Physical,
    DOTPhys,
    DOTMag,
    Both,
    None
}

public enum AttackRangeType
{
    MeleeAttack,
    RangeAttack,
}
#endregion

[Serializable]
public class InfoComponent : BaseSkillComponent
{
    #region InspectorFields
    [SerializeField] protected Schools _school;
    [SerializeField] protected AbilityForm _form;
    [SerializeField] protected DamageType _damageType;
    [SerializeField] protected AttackRangeType _rangeType;
    [SerializeField] protected SkillType _skillType; // в Casting?
    [SerializeField] protected Moving _whileMoving;
    [SerializeField] protected AutoAttack _autoAttack;
    #endregion

    #region RuntimeVariables

    #endregion

    #region Properties
    public Schools School => _school;
    public AbilityForm AbilityForm => _form;
    public DamageType DamageType => _damageType;
    public AttackRangeType AttackRangeType => _rangeType;
    public SkillType SkillType => _skillType;
    public Moving Moving => _whileMoving;
    public AutoAttack AutoAttack => _autoAttack;
    #endregion
}
