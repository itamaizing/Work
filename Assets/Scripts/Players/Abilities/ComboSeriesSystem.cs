using System;
using System.Collections;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ComboSeriesSystem : Skill
{
    [Header("Series Settings")]
    [SerializeField] private float _comboTimeout = 2f;
    [SerializeField] private float _energyPerHit = 5f;
    [SerializeField] private float _energyRestorePercentOnComplete = 0.4f;
    
    private float _speedBonusPerHit = 0.3f;
        
    private float _currentSpeedMultiplier = 1f;
 
    private Energy _energy;
    private RuneComponent _rune;

    private Character _currentTarget;
    private float _timer;
    private int _currentHitCount = 0;
    private float _totalEnergySpentThisSeries = 0f;
    private float _totalRuneSpentThisSeries = 0f;
    private List<AbilityForm> _currentSequence = new();

    private const float BaseSpeedMultiplier = 1.3f;
    private const float BaseRuneRecovery = 1f;

    [SerializeField] private List<SeriesPattern> _availablePatterns = new();

    private bool _isInSeries = false;

    private bool _seriesIsEnable;

    private Skill _lastPreparedSkill;

    #region AdditionalRuneTalent

    private bool _isAdditionalRuneOnSeries;

    public void EnableAdditionalRuneOnSeries(bool value)
    {
        if(_isAdditionalRuneOnSeries == value) return;
        _isAdditionalRuneOnSeries = value;
    }

    #endregion

    #region IncreasedSpeed

    private bool _isSpeedIncreased;

    private const float IncreasedSpeedMultiplier = 1.6f;
    
    public void EnableSpeedIncreasedOnSeries(bool value)
    {
        if(_isSpeedIncreased == value) return;
        _isSpeedIncreased = value;
    }

    #endregion

    #region NewPatterns

    List<AbilityForm> pattern1 = new()
    {
        AbilityForm.Physical,
        AbilityForm.Physical,
        AbilityForm.Magic,
        AbilityForm.Physical
    };
    List<AbilityForm> pattern2 = new()
    {
        AbilityForm.Magic,
        AbilityForm.Magic,
        AbilityForm.Physical,
        AbilityForm.Magic
    };

    private bool _patternsIsAdded;

    public void AddNewPatterns(bool value)
    {
        if(_patternsIsAdded == value) return;
        if (!_patternsIsAdded)
        {
            AddPattern(pattern1, "pattern1");
            AddPattern(pattern2, "pattern2");
        }
        else
        {
            RemovePattern("pattern1");
            RemovePattern("pattern2");
        }

        _patternsIsAdded = value;
    }

    #endregion

    public void EnableSeries(bool value)
    {
        if(_seriesIsEnable == value) return;
        _seriesIsEnable = value;
        
        if (_seriesIsEnable)
            OnEnableSeries();
        else
            OnDisableSeries();
    }

    [System.Serializable]
    public class SeriesPattern
    {
        public string name = "FFFFFF";
        public List<AbilityForm> sequence = new();
        public bool isActive = true;
    }

    private void OnEnableSeries()
    {
        if (_hero?.Abilities == null) return;

        foreach (var skill in _hero.Abilities.Abilities)
        {
            if (skill is IComboSeriesParticipatingSkill comboSkill)
            {
                comboSkill.OnSeriesDamaged -= RegisterHit;
                comboSkill.OnSeriesDamaged += RegisterHit;

                skill.PreparingStarted -= OnSkillPreparingStarted;
                skill.PreparingStarted += OnSkillPreparingStarted;

            }
        }
    }

    private void OnDisableSeries()
    {
        if (_hero?.Abilities == null) return;

        foreach (var skill in _hero.Abilities.Abilities)
        {
            if (skill is IComboSeriesParticipatingSkill comboSkill)
            {
                comboSkill.OnSeriesDamaged -= RegisterHit;
                skill.PreparingStarted -= OnSkillPreparingStarted;
            }
        }
    }

    private void Update()
    {
        if (_isInSeries)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                BreakSeries();
                RefreshPreparedSkill();
            }
        }
    }

    private void OnSkillPreparingStarted(Skill skill)
    {
        if (!_seriesIsEnable || skill == null) return;

        _lastPreparedSkill = skill;
        
        IComboSeriesParticipatingSkill seriesSkill = skill as IComboSeriesParticipatingSkill;
        if (seriesSkill == null) return;

        bool willCompleteSeries = WillCompleteSeriesOnNextHit(skill);

        seriesSkill.OnSeriesPotentialFinal(skill, willCompleteSeries);
    }
    
    private void RefreshPreparedSkill()
    {
        if (_lastPreparedSkill is not IComboSeriesParticipatingSkill comboSkill)
            return;

        comboSkill.OnSeriesPotentialFinal(_lastPreparedSkill, false);
        _lastPreparedSkill = null;
    }
    
    private bool WillCompleteSeriesOnNextHit(Skill skill)
    {
        if (_currentHitCount == 0) return false;

        AbilityForm nextForm = skill.Info.AbilityForm;
        foreach (var pattern in _availablePatterns)
        {
            if (!pattern.isActive) continue;
            if (pattern.sequence.Count != _currentHitCount + 1) continue;

            bool matchesSoFar = true;
            for (int i = 0; i < _currentSequence.Count; i++)
            {
                if (_currentSequence[i] != pattern.sequence[i])
                {
                    matchesSoFar = false;
                    break;
                }
            }

            if (matchesSoFar && pattern.sequence[_currentHitCount] == nextForm)
                return true;
        }

        return false;
    }
    
    private void RegisterHit(GameObject targetGo,Skill skill)
    {
        if(!_seriesIsEnable) return;
        if (skill == null) return;

        if (_energy == null)
            _energy = (Energy)_hero.Resources[ResourceType.Energy];
        
        if(_rune == null)
            _rune = (RuneComponent)_hero.Resources[ResourceType.Rune];

        Character target = targetGo == null ? null : targetGo.GetComponent<Character>();
        IComboSeriesParticipatingSkill series = skill as IComboSeriesParticipatingSkill;

        if (series is { IsTicking: true })
        {
            _totalEnergySpentThisSeries += series.EnergyCostOnHit;
            _timer = _comboTimeout;
            return;
        }

        if (!CanPayEnergy(series)) 
        {
            BreakSeries();
            return;
        }

        PayEnergy(series);
        bool isSameTarget;
        if(target == null)
            isSameTarget = true;
        else
            isSameTarget = target == _currentTarget || _currentTarget == null;

        if (!isSameTarget)
        {
            BreakSeries();
            StartNewSeries(target);
        }

        if (!_isInSeries)
            StartNewSeries(target);

        _currentHitCount++;
        _currentSequence.Add(skill.Info.AbilityForm);
        _timer = _comboTimeout;

        UpdateSpeedBoost();
        series?.OnSeriesHit(_currentHitCount, target);

        RefreshPotentialFinalForPreparedSkill();

        if (CheckPatternCompleted())
        {
            CompleteSeries(series, target);
        }
    }
    
    private void RefreshPotentialFinalForPreparedSkill()
    {
        if (_lastPreparedSkill is not IComboSeriesParticipatingSkill comboSkill) return;
    
        bool willComplete = WillCompleteSeriesOnNextHit(_lastPreparedSkill);
        comboSkill.OnSeriesPotentialFinal(_lastPreparedSkill, willComplete);
    }

    private bool CanPayEnergy(IComboSeriesParticipatingSkill skill)
    {
        if (skill.IgnoresEnergyCostCheck) return true;
        
        float cost = skill.EnergyCostOnHit + _energyPerHit;
        return _energy.CurrentValue >= cost;
    }

    private void PayEnergy(IComboSeriesParticipatingSkill skill)
    {
        float cost = skill.EnergyCostOnHit + _energyPerHit;
        _energy.CmdUse(_energyPerHit);
        _totalEnergySpentThisSeries += cost;
        if(_isAdditionalRuneOnSeries)
            _totalRuneSpentThisSeries += skill.RuneCostOnHit;
    }

    private void StartNewSeries(Character target)
    {
        if(target == null) BreakSeries();
        
        _currentTarget = target;
        _currentHitCount = 0;
        _currentSequence.Clear();
        _totalEnergySpentThisSeries = 0f;
        _totalRuneSpentThisSeries = 0f;

        _isInSeries = true;
        _timer = _comboTimeout;
    }

    private bool CheckPatternCompleted()
    {
        foreach (var pattern in _availablePatterns)
        {
            if (!pattern.isActive || pattern.sequence.Count != _currentSequence.Count) 
                continue;

            if (pattern.sequence.SequenceEqual(_currentSequence))
                return true;
        }
        return false;
    }

    private void CompleteSeries(IComboSeriesParticipatingSkill lastSkill, Character target)
    {
        float restored = _totalEnergySpentThisSeries * _energyRestorePercentOnComplete;
        _energy.CmdAdd(restored);

        if (_isAdditionalRuneOnSeries)
        {
            _rune.CmdAdd(_totalRuneSpentThisSeries + BaseRuneRecovery);
            _totalRuneSpentThisSeries = 0f;
        }

        lastSkill.OnSeriesCompleted(target, _currentHitCount, _totalEnergySpentThisSeries);
        
        ResetSeries();
    }

    private void BreakSeries()
    {
        ResetSeries();
    }

    private void UpdateSpeedBoost()
    {
        ResetSpeedBoost();

        if (_currentHitCount < 2)
        {
            _currentSpeedMultiplier = 1f;
        }
        else if (_isSpeedIncreased)
        {
            _currentSpeedMultiplier = IncreasedSpeedMultiplier;

            if (_currentHitCount > 2)
            {
                _currentSpeedMultiplier += _speedBonusPerHit * (_currentHitCount - 2);
            }
        }
        else
        {
            _currentSpeedMultiplier = BaseSpeedMultiplier + 
                                      _speedBonusPerHit * (_currentHitCount - 2);
        }

        ApplyCurrentSpeedBoost();
    }

    private void ApplyCurrentSpeedBoost()
    {
        foreach (var skill in _hero.Abilities.Abilities)
        {
            if (skill is IComboSeriesParticipatingSkill)
            {
                skill.Buff.AttackSpeed.IncreasePercentage(_currentSpeedMultiplier);
                skill.Buff.CastSpeed.IncreasePercentage(_currentSpeedMultiplier);
            }
        }
    }

    private void ResetSeries()
    {
        _currentTarget = null;
        _currentHitCount = 0;
        _currentSequence.Clear();
        _totalEnergySpentThisSeries = 0f;
        _totalRuneSpentThisSeries = 0f;
        _currentSpeedMultiplier = 1f;
        _isInSeries = false;
        ResetSpeedBoost();
    }

    private void ResetSpeedBoost()
    {
        foreach (var skill in _hero.Abilities.Abilities)
        {
            if (skill is IComboSeriesParticipatingSkill)
            {
                skill.Buff.AttackSpeed.Reset();
                skill.Buff.CastSpeed.Reset();
            }
        }
    }
    
    private void AddPattern(List<AbilityForm> sequence, string name = "")
    {
        if (sequence == null || sequence.Count == 0)
            return;

        _availablePatterns.Add(new SeriesPattern
        {
            name = string.IsNullOrEmpty(name) ? $"Pattern_{_availablePatterns.Count + 1}" : name,
            sequence = new List<AbilityForm>(sequence),
            isActive = true
        });
    }

    private void RemovePattern(string name)
    {
        if (string.IsNullOrEmpty(name))
            return;

        int removedCount = _availablePatterns.RemoveAll(pattern => 
            pattern.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    #region Skill

    protected override IEnumerator CastJob()
    {
        throw new NotImplementedException();
    }

    protected override int AnimTriggerCastDelay { get; }
    protected override int AnimTriggerCast { get; }
    #endregion
}