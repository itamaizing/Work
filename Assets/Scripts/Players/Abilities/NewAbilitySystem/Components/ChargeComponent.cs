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
    private bool _isServer = false;
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
    public event Action<int> OnMaxChange;
    public event Action<int> OnCurrentChange;
    public event Action<float> OnRechargeStart;
    public event Action<int> OnRechargeEnd;
    #endregion Events

    
    #region Methods

    public void Init(Skill skill, bool isServer)
    {
        if (isInitialized)
            return;

        base.Init(skill);

        RechargeTimers.Callback += OnChargeChange;
        skill_attr = _skillAttributes[SkillAttributeName.Cooldown];
        char_attr = _characterAttributes[CharacterAttributeName.CooldownReduction];
        _isServer = isServer;
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
                cdTime = _skillAttributes.GetCombined(skill_attr, char_attr, _baseCooldown);
            _skill.CmdStartRecharge(cdTime);
        }
        else
        {
            OnCurrentChange?.Invoke(RemainingCharges);
        }
        OnMaxChange?.Invoke(_maxCharges);
    }

    public void ModifyMax(int delta, bool cooldowned=false)
    {
        _maxCharges += delta;

        if (_maxCharges < 0) //1?
            _maxCharges = 0;

        if (cooldowned)
        {
            float cdTime = _baseCooldown;
            if (affectedByCDR)
                cdTime = _skillAttributes.GetCombined(skill_attr, char_attr, _baseCooldown);

            for (int i = 0; i < delta; i++)
                _skill.CmdStartRecharge(cdTime);
        }
        else
        {
        //Нужно ли заканчивать уже идущие? Как будто не стоит, иначе перепрокачка будет сбрасывать КД. Но сейчас оно может уйти в отриц. значения
            OnCurrentChange?.Invoke(RemainingCharges);
        }
        OnMaxChange?.Invoke(_maxCharges);
    }

    public void RestoreCharge(int index)
    {
        if (RemainingCharges < _maxCharges)
        {
            //OnCurrentChange?.Invoke(RemainingCharges);
            //OnRechargeEnd?.Invoke(index); //считываются ниже OnChargeChange
            Debug.Log("Recharged " + index);
            //if (_isServer)
            _skill.CmdEndRecharge(index);
        }
    }

    public void ModifyDuration(float delta, bool tickAll = false)
    {
        _skill.CmdModifyRechargeTime(delta, tickAll);
    }

    public bool TryUse()
    {
        if (RemainingCharges <= 0)
            return false;

        float cdTime = _baseCooldown;
        if (affectedByCDR)
        {
            cdTime = _skillAttributes.GetCombined(skill_attr, char_attr, _baseCooldown);
        }

        //Debug.Log("Recgharge time is " + cdTime);
        StartRecharge(cdTime);
        return true;
    }

    private void StartRecharge(float rechargeTime)
    {
        //OnRechargeStart?.Invoke(rechargeTime);
        _skill.CmdStartRecharge(rechargeTime);
    }

    //Если сломается - хардкодим кол-во зарядов в списке, добавляем id текущего заряда на перезарядке
    public void OnChargeChange(SyncList<double>.Operation op, int index, double oldTime, double newTime)
    {
        if (!_isServer)
            return;

        switch (op)
        {
            case SyncList<double>.Operation.OP_ADD:
                OnRechargeStart?.Invoke((float) (newTime - NetworkTime.time));
                Debug.Log("RECHARGE STARTED " + (newTime - NetworkTime.time));
                break;

            case SyncList<double>.Operation.OP_REMOVEAT:
                OnRechargeEnd?.Invoke(index); //Это не тот индекс ?
                Debug.Log("RECHARGE ENDED! " + index);
                break;
        }

        OnCurrentChange?.Invoke(RemainingCharges);
    }
    #endregion Methods
}
