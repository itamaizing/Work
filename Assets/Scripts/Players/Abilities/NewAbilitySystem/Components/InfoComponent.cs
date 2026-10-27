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
    Magic,
    Physical,
    Mixed,
}

public enum Moving
{
    UntilFinish,
    UntilCast,
    Free
}

public enum AutoAttack
{
    autoAttack,
    nonAutoAttack
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
    [SerializeField] private Schools _school;
    [SerializeField] protected AbilityForm _form;
    [SerializeField] protected DamageType _damageType;
    [SerializeField] protected AttackRangeType _rangeType;
    [SerializeField] protected Moving _whileMoving;
    [SerializeField] protected AutoAttack _autoAttack;
    #endregion

    #region RuntimeVariables

    #endregion

    #region Properties
    public Schools School
    {
        get { return _school; }
        set { _school = value; }
    }
    public AbilityForm AbilityForm => _form;
    public DamageType DamageType => _damageType;
    public AttackRangeType AttackRangeType => _rangeType;
    public Moving Moving => _whileMoving;
    public AutoAttack AutoAttack => _autoAttack;
    #endregion
}
