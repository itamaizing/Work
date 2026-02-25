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
    #region InspectorFields
    [SerializeField] private bool _usesCharges;
    [SerializeField] protected bool _isComboPart;
    [SerializeField] private ChargeCooldownType _cooldownType;

    [SerializeField] protected int _maxCharges;
    [SerializeField] protected float _baseCooldown;
    [SerializeField] protected bool affectedByCDR;
    #endregion

    #region RuntimeVariables
    private int _currentCharges;
    private List<float> _remainingCooldowns = new();
    #endregion

    #region Properties
    public bool UsesCharges => _usesCharges;
    public bool IsComboPart => _isComboPart;
    public ChargeCooldownType CooldownType => _cooldownType;
    public int MaxCharges  {
        get { return _maxCharges; }
        set { _maxCharges = value; }
    }
    public int CurrentCharges  {
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

    #region Events

    #endregion Events
    public event Action<int> MaxChargesChanged;
    public event Action<int> CurrentChargesChanged;
    public event Action<float> OnRechargeStart;
    public event Action<int> OnRechargeEnd;
    #region Methods

    //Переписать на ModifyMax
    //Если список перезарядок не пустой - оставить КД
    //Если пустой - проверять cooldowned? Или просто вырезать его и добавлять без КД?
    public void AddMax(bool cooldowned=false)
    {
        _maxCharges += 1;
        if (cooldowned)
        {
            _remainingCooldowns.Add(_baseCooldown);
        }
        else
        {
            _currentCharges++;
            CurrentChargesChanged?.Invoke(_currentCharges);
        }
        MaxChargesChanged?.Invoke(_maxCharges);
    }

    public void RestoreCharge()
    {
        if (_currentCharges < _maxCharges)
        {
            _currentCharges++;
            CurrentChargesChanged?.Invoke(_currentCharges);
        }
    }


    public bool TryUse()
    {
        if (_currentCharges <= 0)
            return false;

        _currentCharges--;
        float cdTime = _baseCooldown;
        if (affectedByCDR)
        {
            cdTime = _skillAttributes.Attributes[SkillAttributeName.Cooldown].CalculateFor(_baseCooldown);
        }
        StartRecharge(cdTime);
        return true;
    }

    private void StartRecharge(float rechargeTime)
    {
        OnRechargeStart?.Invoke(rechargeTime);
        _currentCharges -= 1;
        _remainingCooldowns.Add(rechargeTime);
    }

    //Если сломается - хардкодим кол-во зарядов в списке, добавляем id текущего заряда на перезарядке
    public void Tick(float time)
    {
        if (_remainingCooldowns.Count <= 0)
            return;
        switch (_cooldownType)
        {
            case ChargeCooldownType.Sequential:
                TickCurrent(time);
                break;
            case ChargeCooldownType.Independant:
                TickAll(time);
                break;
        }
    }

    private void TickCurrent(float time)
    {
        _remainingCooldowns[0] -= time;
        if (_remainingCooldowns[0] <= 0)
        {
            RestoreCharge();
            _remainingCooldowns.RemoveAt(0);
        }
    }

    private void TickAll(float time)
    {
        for (int i = _remainingCooldowns.Count - 1; i >= 0; i--)
        {
            _remainingCooldowns[i] -= time;

            if (_remainingCooldowns[i] <= 0)
            {
                RestoreCharge();
                OnRechargeEnd?.Invoke(i);
                _remainingCooldowns.RemoveAt(i);
            }
        }
    }
    #endregion Methods
}
