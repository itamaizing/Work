using Mirror;
using System;
using UnityEngine;

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
    //private float _remainingTime;
    /// <summary>
    /// Актуальная база (до модификаторов)
    /// </summary>
    private double _currentMax;
    private bool _isActive = false;
    private bool isSyncronized = false;
    #endregion

    #region Properties
    public float CooldownTime {
        get { return _skillAttributes.Cooldown; }
        set { _skillAttributes[SkillAttributeName.Cooldown].SetBaseValue(value); }
    }
    //public bool IsActive =>  NetworkTime.time < _skill.CooldownEnd;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            //OnChanged
        }
    }
    public double RemainingTime => Mathf.Max(0, (float)(_skill.CooldownEnd - NetworkTime.time));
    public double ElapsedTime => _currentMax - RemainingTime;
    #endregion

    #region Events
    public event Action<double> OnCooldownStart;
    public event Action<double, double> OnCooldownModify;
    public event Action OnCooldownEnd;
    #endregion Events

    #region Methods
    public override void Init(Skill skill)
    {
        base.Init(skill);
        _isActive = false;
        CooldownTime = _baseCooldown;
        _currentMax = CooldownTime;
    }

    public void Tick()
    {
        if (isSyncronized && IsActive && NetworkTime.time > _skill.CooldownEnd)
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
    /// <param name="canOvershoot">Can new duration be higher than maxCD</param>
    /// </summary>
    //public void ModifyRemaining(float delta, bool canOvershoot=false)
    //{
    //    if (!IsActive)
    //        return;

    //    _remainingTime += delta;
    //    if (_remainingTime <= 0)
    //        EndCooldown();
    //    if (_remainingTime > _currentMax && !canOvershoot)
    //        _remainingTime = _currentMax;
    //    _skill.CmdCooldownModify(_remainingTime);
    //    OnCooldownModify?.Invoke(_remainingTime, _currentMax);
    //}

    public void SetRemaining(float duration, bool canIncrease = false)
    {
        if (!IsActive || (duration > RemainingTime && !canIncrease))
            return;
        if (_currentMax < duration)
            _currentMax = duration;
        _skill.CmdCooldownStart(duration);
        OnCooldownModify?.Invoke(duration, _currentMax);
    }

    public void ForceEnd()
    {
        EndCooldown();
    }

    public float CalculateValue(float time)
    {
        return _skillAttributes.GetCombined(time, _skillAttributes[SkillAttributeName.Cooldown],
            _attributes[CharacterAttributeName.CooldownReduction]);
        // Обновить, когда на персонаже появится атрибут КД
        //return _skillAttributes.Attributes[SkillAttributeName.Cooldown].CalculateFor(time);
    }

    private void StartCooldown(float duration)
    {
        Debug.Log($"{duration} CD STARTED!!");
        Debug.Log($"{NetworkTime.time}");
        //_remainingTime = duration;
        isSyncronized = false;
        _currentMax = duration;
        _skill.CmdCooldownStart(duration);
        OnCooldownStart?.Invoke(duration);
        IsActive = true;
    }

    private void EndCooldown()
    {
        Debug.Log("CD ENDED!!!");
        Debug.Log($"{NetworkTime.time}. {_skill.CooldownEnd}");
        if (!IsActive)
            return;
        //_remainingTime = 0;
        _currentMax = _skillAttributes.Cooldown;
        _skill.CmdCooldownEnd();
        OnCooldownEnd?.Invoke();
        IsActive = false;
    }

    public void OnServerCooldownChanged(double oldValue, double newValue)
    {
        isSyncronized = true;
    }
    #endregion Methods
}