using Mirror;
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
    Attribute char_attr, skill_attr;
    private bool isInitialized = false;
    #endregion

    #region Properties
    public bool UsesCharges => _usesCharges;
    public bool IsComboPart => _isComboPart;
    public ChargeCooldownType CooldownType => _cooldownType;
    public float BaseCooldown => _baseCooldown;
    public int MaxCharges {
        get { return _maxCharges; }
        set { _maxCharges = value; }
    }
    public SyncList<double> RechargeTimers => _skill.RechargeTimers;
    public int RemainingCharges => (MaxCharges - RechargeTimers.Count);
    public bool HasCharges
    {
        get
        { if (!_usesCharges)
                return true;
            else
            {
                if (_isComboPart) return true;
                return (RemainingCharges > 0);
            }
        }
    }
    #endregion

    #region Events

    #endregion Events
    public event Action<int> MaxChargesChanged;
    public event Action<int> CurrentChargesChanged;
    public event Action<float> OnRechargeStart;
    public event Action<int> OnRechargeEnd;
    #region Methods

    public override void Init(Skill skill)
    {
        base.Init(skill);

        RechargeTimers.Callback += OnChargeChange;
        skill_attr = _skillAttributes[SkillAttributeName.Cooldown];
        char_attr = _attributes[CharacterAttributeName.CooldownReduction];
        isInitialized = true;
    }

    public void Tick()
    {
        if (!isInitialized)
            return;
        for (int i = RechargeTimers.Count - 1; i >= 0; i--)
            if (RechargeTimers[i] <= NetworkTime.time)
                RestoreCharge(i);
    }

    //Переписать на ModifyMax
    //Если список перезарядок не пустой - оставить КД
    //Если пустой - проверять cooldowned? Или просто вырезать его и добавлять без КД?
    public void AddMax(bool cooldowned=false)
    {
        _maxCharges += 1;
        if (cooldowned)
        {
            float cdTime = _baseCooldown;
            if (affectedByCDR) 
                cdTime = _skillAttributes.GetCombined(_baseCooldown, skill_attr, char_attr);
            _skill.CmdStartRecharge(cdTime);
        }
        else
        {
            CurrentChargesChanged?.Invoke(RemainingCharges);
        }
        MaxChargesChanged?.Invoke(_maxCharges);
    }

    public void RestoreCharge(int index)
    {
        if (RemainingCharges < _maxCharges)
        {
            CurrentChargesChanged?.Invoke(RemainingCharges);
            _skill.CmdEndRecharge(index);
        }
    }

    public bool TryUse()
    {
        Debug.Log("trying to use a charge");
        if (RemainingCharges <= 0)
            return false;

        float cdTime = _baseCooldown;
        if (affectedByCDR)
        {
            cdTime = _skillAttributes.GetCombined(_baseCooldown, skill_attr, char_attr);
        }
        StartRecharge(cdTime);
        return true;
    }

    private void StartRecharge(float rechargeTime)
    {
        OnRechargeStart?.Invoke(rechargeTime);
        _skill.CmdStartRecharge(rechargeTime);
    }

    //Если сломается - хардкодим кол-во зарядов в списке, добавляем id текущего заряда на перезарядке
    public void OnChargeChange(SyncList<double>.Operation op, int index, double oldTime, double newTime)
    {
        switch (op)
        {
            case SyncList<double>.Operation.OP_ADD:
                OnRechargeStart?.Invoke((float) (newTime - NetworkTime.time));
                Debug.Log("RECHARGE STARTED " + (newTime - NetworkTime.time));
                break;

            case SyncList<double>.Operation.OP_REMOVEAT:
                OnRechargeEnd?.Invoke(index); //Это не тот индекс
                Debug.Log("RECHARGE ENDED! " + index);
                break;
        }

        CurrentChargesChanged?.Invoke(RemainingCharges);
    }
    #endregion Methods
}
