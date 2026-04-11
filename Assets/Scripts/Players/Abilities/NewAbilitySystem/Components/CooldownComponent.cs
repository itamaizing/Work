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
    public float BaseCooldownTime => _baseCooldown;
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
    public float RemainingTime => _skill != null ? Mathf.Max(0, (float)(_skill.CooldownEnd - NetworkTime.time)) : 0;
    public double ElapsedTime => _currentMax - RemainingTime;
    #endregion

    #region Events
    public event Action<float> OnStart;
    public event Action<double, double> OnModify;
    public event Action OnEnd;
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
    /// Positive: Increases Remaining Time.
    /// Negative: Decreases Remaining Time.
    /// <param name="canOvershoot">Can new duration be higher than maxCD</param>
    /// </summary>
    public void Modify(float delta, bool canOvershoot = false)
    {
        if (!IsActive || (RemainingTime + delta > _currentMax && !canOvershoot))
            return;

        isSyncronized = false;

        Debug.Log($"CD Modify: {RemainingTime}+{delta}");
        _skill.CmdCooldownModify(delta);
        OnModify?.Invoke(RemainingTime + delta, _currentMax);

    }

    public void SetRemaining(float duration, bool canIncrease = false, bool shouldModify = false)
    {
        if (shouldModify)
            duration = CalculateValue(duration);

        if (!IsActive || (duration > RemainingTime && !canIncrease))
            return;
        
        isSyncronized = false;

        if (_currentMax < duration)
            _currentMax = duration;
        _skill.CmdCooldownStart(duration);
        OnModify?.Invoke(duration, _currentMax);
    }

    public void SetIncreased(float duration, bool shouldModify = false)
    {
        if (shouldModify)
            duration = CalculateValue(duration);

        if (!IsActive || (duration < RemainingTime))
            return;

        isSyncronized = false;

        if (_currentMax < duration)
            _currentMax = duration;
        _skill.CmdCooldownStart(duration);
        OnModify?.Invoke(duration, _currentMax);
    }

    public void SetReduced(float duration, bool shouldModify = false)
    {
        if (shouldModify)
            duration = CalculateValue(duration);

        if (!IsActive || (duration > RemainingTime))
            return;

        isSyncronized = false;

        if (_currentMax < duration)
            _currentMax = duration;
        _skill.CmdCooldownStart(duration);
        OnModify?.Invoke(duration, _currentMax);
    }

    public void ForceEnd()
    {
        EndCooldown();
    }

    public float CalculateValue(float time)
    {
        if (_characterAttributes != null)
            return _skillAttributes.GetCombined(_skillAttributes[SkillAttributeName.Cooldown],
                _characterAttributes[CharacterAttributeName.CooldownReduction],
                time);
        return _skillAttributes[SkillAttributeName.Cooldown].CalculateFor(time);
    }

    private void StartCooldown(float duration)
    {
        //Debug.Log($"{duration} CD STARTED!!");
        isSyncronized = false;
        _currentMax = duration;
        _skill.CmdCooldownStart(duration);
        OnStart?.Invoke(duration);
        IsActive = true;
    }

    private void EndCooldown()
    {
        if (!IsActive || !isSyncronized)
            return;
        //Debug.Log("CD ENDED!!!");
        _currentMax = _skillAttributes.Cooldown;
        _skill.CmdCooldownEnd();
        OnEnd?.Invoke();
        IsActive = false;
    }

    public void OnServerCooldownChanged(double oldValue, double newValue)
    {
        isSyncronized = true;
    }
    #endregion Methods
}