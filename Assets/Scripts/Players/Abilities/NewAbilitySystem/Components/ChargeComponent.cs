using System;
using System.Collections.Generic;
using UnityEngine;


public enum ChargeCooldownType
{
    /// <summary>
    /// Кдшатся параллельно друг другу
    /// </summary>
    Independant,
    /// <summary>
    /// Кдшатся последовательно один после другого
    /// </summary>
    Sequential,
}

[Serializable]
public class ChargeComponent : BaseSkillComponent
{
    #region Fields
    [SerializeField] private bool _usesCharges;
    [SerializeField] protected bool _isComboPart;
    [SerializeField] private ChargeCooldownType _cooldownType;

    [SerializeField] protected int _maxCharges;
    private int _currentCharges;
    [SerializeField] protected float _baseCooldown;

    //Подумать над другой архитектурой? Перевести на список корутин?
    private List<float> _remainingCooldowns = new();
    #endregion

    #region Properties
    public bool UsesCharges => _usesCharges;
    public bool IsComboPart => _isComboPart;
    public ChargeCooldownType CooldownType => _cooldownType;
    public int MaxCharges
    {
        get { return _maxCharges; }
        set { _maxCharges = value; }
    }
    public int CurrentCharges
    {
        get { return _currentCharges; }
        set {
            _currentCharges = value;
            //_skill.CurrentChargeChanged?.Invoke(_currentCharges);
        }
    }
    public bool HasCharges => _currentCharges > 0;
    public float BaseCooldown => _baseCooldown;

    public List<float> ActiveCooldowns => _remainingCooldowns;
    #endregion
}
