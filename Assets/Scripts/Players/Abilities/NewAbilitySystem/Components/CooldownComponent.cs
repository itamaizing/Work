using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[Serializable]
public class CooldownComponent : BaseSkillComponent
{
    #region InspectorFields
    /// <summary>
    /// Базовое значение кулдауна. Нельзя менять
    /// </summary>
    [SerializeField] protected float _baseCooldown;
    #endregion

    #region Runtime Variables
    private float _remainingTime;
    /// <summary>
    /// Актуальная база (до модификаторов)
    /// </summary>
    private float _currentMax;
    #endregion

    #region Properties
    public float CooldownTime {
        get { return _skillAttributes.Cooldown; }
        set { _skillAttributes.Attributes[SkillAttributeName.Cooldown].SetBaseValue(value); }
    }
    public float RemainingTime => _remainingTime;
    public float ElapsedTime => _currentMax - _remainingTime;
    public bool CooldownActive => _remainingTime > 0;
    #endregion

    #region Events
    public event Action<float> OnCooldownStart;
    public event Action<float, float> OnCooldownModify;
    public event Action OnCooldownEnd;
    #endregion Events

    #region Methods
    public override void Init(Skill skill)
    {
        base.Init(skill);
        CooldownTime = _baseCooldown;
        _currentMax = CooldownTime;
    }
    public void Tick(float time)
    {
        if (!CooldownActive)
            return;
        _remainingTime -= time;
        if (_remainingTime <= 0)
        {
            EndCooldown();
        }
    }

    public void Start()
    {
        StartCooldown(CooldownTime);
    }

    public void StartCustom(float time, bool shouldCalculate=false)
    {
        if (shouldCalculate)
        {
            StartCooldown(CalculateValue(time));
        }
        else
        {
            StartCooldown(time);
        }
    }
    
    /// <summary>
    /// Positive: Increase Remaining Time.
    /// Negative: Decrease Remaining Time.
    /// <param name="canOvershoot">Can new time be higher than maxCD</param>
    /// </summary>
    public void ModifyRemaining(float time, bool canOvershoot=false)
    {
        _remainingTime += time;
        if (_remainingTime <= 0)
            EndCooldown();
        if (_remainingTime > _currentMax && !canOvershoot)
            _remainingTime = _currentMax;
        OnCooldownModify?.Invoke(_remainingTime, _currentMax);
    }

    public void SetRemaining(float time, bool canIncrease = false)
    {
        if (time > _remainingTime && !canIncrease)
            return;
        _remainingTime = time;
        if (_remainingTime <= 0)
        {
            EndCooldown();
            return;
        } OnCooldownModify?.Invoke(_remainingTime, _currentMax);
    }

    public void ForceEnd()
    {
        EndCooldown();
    }

    public float CalculateValue(float time)
    {
        // Обновить, когда на персонаже появится атрибут КД
        return _skillAttributes.Attributes[SkillAttributeName.Cooldown].CalculateFor(time);
    }

    private void StartCooldown(float time)
    {
        _remainingTime = time;
        _currentMax = time;
        OnCooldownStart?.Invoke(time);
    }

    private void EndCooldown()
    {
        _remainingTime = 0;
        _currentMax = _skillAttributes.Cooldown;
        OnCooldownEnd?.Invoke();
    }
    #endregion Methods
}