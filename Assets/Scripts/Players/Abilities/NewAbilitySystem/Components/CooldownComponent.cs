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
        get { return _skillAttributes != null ? _skillAttributes.Cooldown : _baseCooldown; }
        set { _skillAttributes[SkillAttributeName.Cooldown].SetBaseValue(value); }
    }
    //public bool IsActive =>  NetworkTime.time < _skill.CooldownEnd;
    public bool IsActive
    {
        get => _isActive;
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

    public void Start()
    {
        //StartCooldown(CooldownTime); //заметил, что всегда делали именно SetIncreased ¯\_(ツ)_/¯
        SetIncreased(CooldownTime, shouldModify: false, opModify: false);
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

    public void ForceEnd()
    {
        EndCooldown();
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

        float newRemaining = Mathf.Max(0f, RemainingTime + delta);

        isSyncronized = false;
        _skill.CmdCooldownModify(delta);

        if (newRemaining <= 0f)
        {
            _isActive = false;
            _currentMax = _skillAttributes != null ? _skillAttributes.Cooldown : _baseCooldown;
            OnEnd?.Invoke();
        }
        else
        {
            OnModify?.Invoke(newRemaining, _currentMax);
        }
    }

    /// <summary>
    /// Set remaining duration
    /// </summary>
    public void SetRemaining(float duration, bool canIncrease = false, bool shouldModify = false, bool opModify = true)
    {
        if (shouldModify)
            duration = CalculateValue(duration);

        if (!IsActive || (duration > RemainingTime && !canIncrease))
            return;
        
        StartCooldown(duration, opModify);
    }

    /// <summary>
    /// Set remaining duration HIGHER than current
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="shouldModify"></param>
    public void SetIncreased(float duration, bool shouldModify = false, bool opModify = false)
    {
        if (shouldModify)
            duration = CalculateValue(duration);

        if (duration < RemainingTime)
            return;

        StartCooldown(duration, opModify);
    }

    /// <summary>
    /// Set remaining duration LOWER than current
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="shouldModify"></param>
    public void SetReduced(float duration, bool shouldModify = false, bool opModify = false)
    {
        if (shouldModify)
            duration = CalculateValue(duration);

        if (duration > RemainingTime)
            return;

        StartCooldown(duration, opModify);
    }

    public float CalculateValue(float time)
    {
        if (_characterAttributes != null)
            return _skillAttributes.GetCombined(_skillAttributes[SkillAttributeName.Cooldown],
                _characterAttributes[CharacterAttributeName.CooldownReduction],
                time);
        return _skillAttributes[SkillAttributeName.Cooldown].CalculateFor(time);
    }

    private void StartCooldown(float duration, bool opModify=false)
    {
        //Debug.Log($"{duration} CD STARTED");
        isSyncronized = false;
        _skill.CmdCooldownStart(duration);
        if (!opModify)
        {
            _currentMax = duration;
            OnStart?.Invoke(duration);
        }
        else
        {
            if (_currentMax < duration)
                _currentMax = duration;
            OnModify?.Invoke(duration, _currentMax);
        }
        _isActive = true;
    }

    private void EndCooldown()
    {
        if (!IsActive || !isSyncronized)
            return;
        //Debug.Log("CD ENDED");
        _currentMax = _skillAttributes.Cooldown;
        _skill.CmdCooldownEnd();
        OnEnd?.Invoke();
        _isActive = false;
    }

    public void OnServerCooldownChanged(double oldValue, double newValue)
    {
        isSyncronized = true;
    }
    #endregion Methods
}