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

    [SyncVar(hook = nameof(OnSeriesSpeedChanged))]
    private float _syncedSeriesSpeed = 1f;
    
    private AttributeModifier _speedBonusModifier;
 
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
    
    private bool _timerPaused = false;

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        ApplySeriesSpeed(_syncedSeriesSpeed);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplySeriesSpeed(_syncedSeriesSpeed);
    }

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

    #region SeriesBuff

    private void RefreshSeriesBuff()
    {
        if (_hero == null) return;

        bool active = _isInSeries && _currentHitCount > 0 && _currentSequence.Count > 0;

        bool magic = active && _currentSequence[_currentSequence.Count - 1] == AbilityForm.Magic;

        CmdSyncSeriesBuff(active ? _currentHitCount : 0, magic, Mathf.Max(0f, _timer), _comboTimeout, _timerPaused);
    }

    [Command]
    private void CmdSyncSeriesBuff(int hitCount, bool magic, float remaining, float maximum, bool paused)
    {
        ApplySeriesBuffSnapshot(hitCount, magic, remaining, maximum, paused);

        RpcSyncSeriesBuff(hitCount, magic, remaining, maximum, paused);
    }

    [ClientRpc]
    private void RpcSyncSeriesBuff(int hitCount, bool magic, float remaining, float maximum, bool paused)
    {
        if (isServer) return;

        ApplySeriesBuffSnapshot(hitCount, magic, remaining, maximum, paused);
    }

    private void ApplySeriesBuffSnapshot(int hitCount, bool magic, float remaining, float maximum, bool paused)
    {
        if (_hero == null || _hero.CharacterState == null) return;

        CharacterState states = _hero.CharacterState;

        if (hitCount <= 0)
        {
            RemoveSeriesBuffLocal(states, States.IceDeathSeriesPhysical);
            RemoveSeriesBuffLocal(states, States.IceDeathSeriesMagic);
            return;
        }

        States active = magic ? States.IceDeathSeriesMagic : States.IceDeathSeriesPhysical;

        States opposite = magic ? States.IceDeathSeriesPhysical : States.IceDeathSeriesMagic;

        RemoveSeriesBuffLocal(states, opposite);

        if (!states.enumToState.ContainsKey(active))
            states.enumToState.Add(active, new IceDeathSeriesState(active));

        if (states.GetState(active) is IceDeathSeriesState current)
        {
            current.SetSnapshot(hitCount, remaining, maximum, paused);
            return;
        }

        var template = (IceDeathSeriesState)states.enumToState[active];
        template.SetSnapshot(hitCount, remaining, maximum, paused);

        states.AddStateLogic(active, maximum, 0f, Schools.None, _hero.gameObject, nameof(ComboSeriesSystem), isCanDodgeMagState: true);

        if (states.GetState(active) is IceDeathSeriesState added)
            added.SetSnapshot(hitCount, remaining, maximum, paused);
    }

    private static void RemoveSeriesBuffLocal(CharacterState states, States id)
    {
        StateBasic state = states.GetState(id);

        if (state != null)
            state.ExitState();
    }

    #endregion

    #region BoostWindow

    private readonly HashSet<Skill> _seriesBoostedSkills = new();

    private void RefreshSeriesBoosts()
    {
        if (!isOwned) return;

        var nextBoostedSkills = new HashSet<Skill>();

        if (_seriesIsEnable && _isInSeries && _hero?.Abilities != null)
        {
            foreach (var skill in _hero.Abilities.Abilities)
            {
                if (skill == null || !skill.IsSkillActive)
                    continue;

                if (skill is not IComboSeriesParticipatingSkill)
                    continue;

                if (WillCompleteSeriesOnNextHit(skill))
                    nextBoostedSkills.Add(skill);
            }
        }

        foreach (var skill in _seriesBoostedSkills.ToArray())
        {
            if (nextBoostedSkills.Contains(skill))
                continue;

            _seriesBoostedSkills.Remove(skill);

            if (skill != null)
                skill.DisableSkillBoost();
        }

        foreach (var skill in nextBoostedSkills)
        {
            if (_seriesBoostedSkills.Add(skill))
                skill.EnableSkillBoost();
        }
    }

    private void ClearSeriesBoosts()
    {
        foreach (var skill in _seriesBoostedSkills)
        {
            if (skill != null)
                skill.DisableSkillBoost();
        }

        _seriesBoostedSkills.Clear();
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

                skill.TargetingStarted -= OnSkillTargetingStarted;
                skill.TargetingStarted += OnSkillTargetingStarted;

                skill.CastStarted -= OnSkillCastStarted;
                skill.CastStarted += OnSkillCastStarted;

                skill.OnSkillCanceled -= OnSkillCanceledHandler;
                skill.OnSkillCanceled += OnSkillCanceledHandler;
            }
        }
        
        RefreshSeriesBoosts();
    }

    private void OnDisableSeries()
    {
        BreakSeries();
        RefreshPreparedSkill();

        if (_hero?.Abilities == null) return;

        foreach (var skill in _hero.Abilities.Abilities)
        {
            if (skill is IComboSeriesParticipatingSkill comboSkill)
            {
                comboSkill.OnSeriesDamaged -= RegisterHit;
                skill.TargetingStarted -= OnSkillTargetingStarted;
                skill.CastStarted -= OnSkillCastStarted;
                skill.OnSkillCanceled -= OnSkillCanceledHandler;
            }
        }
    }
    
    private void OnDisable()
    {
        ClearSeriesBoosts();
    }
    
    private void Update()
    {
        if (_isInSeries && !_timerPaused)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                BreakSeries();
                RefreshPreparedSkill();
            }
        }
    }
    
    private void OnSkillCastStarted()
    {
        if (!_seriesIsEnable || !_isInSeries) return;
        _timerPaused = true;
        RefreshSeriesBuff();
    }

    private void OnSkillCanceledHandler()
    {
        if (!_seriesIsEnable || !_isInSeries) return;
        _timerPaused = false;
        RefreshSeriesBuff();
    }

    private void OnSkillTargetingStarted(Skill skill)
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
        if (!_seriesIsEnable) return;
        if (skill == null) return;
        
        _timerPaused = false;

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
            RefreshSeriesBuff();
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
        RefreshSeriesBuff();

        UpdateSpeedBoost();
        series?.OnSeriesHit(_currentHitCount, target);

        RefreshPotentialFinalForPreparedSkill();

        if (CheckPatternCompleted())
        {
            CompleteSeries(series, target);
        }
        else
        {
            RefreshSeriesBoosts();
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
        if (_isAdditionalRuneOnSeries)
        {
            _totalRuneSpentThisSeries += skill.RuneCostOnHit;
        }
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
            float runeToAdd = (_totalRuneSpentThisSeries * 2) + BaseRuneRecovery;
            _rune.CmdAdd(runeToAdd);
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
        if (_currentHitCount < 2)
        {
            ApplySeriesSpeed(1f);
            CmdResetSpeedBoost();
            return;
        }
        
        float bonus = _speedBonusPerHit * (_currentHitCount - 1);

        if (_isSpeedIncreased)
        {
            bonus += (IncreasedSpeedMultiplier - 1f);
        }

        _currentSpeedMultiplier = 1f + bonus;

        ApplySeriesSpeed(_currentSpeedMultiplier);
        CmdApplySpeedBoost(_currentSpeedMultiplier);
    }

    private void OnSeriesSpeedChanged(float previous, float current)
    {
        if (isServer) return;

        ApplySeriesSpeed(current);
    }

    private void ApplySeriesSpeed(float multiplier)
    {
        if (_hero == null || _hero.AttributeSystem == null) return;

        var castSpeedAttr = _hero.AttributeSystem[CharacterAttributeName.CastSpeed];

        if (Mathf.Approximately(multiplier, 1f))
        {
            if (_speedBonusModifier != null)
            {
                castSpeedAttr.RemoveModifier(_speedBonusModifier);
                _speedBonusModifier = null;
            }

            return;
        }

        if (_speedBonusModifier == null)
        {
            _speedBonusModifier = new AttributeModifier(multiplier, ModifierType.Multiplier, this);
            castSpeedAttr.AddModifier(_speedBonusModifier);
        }
        else
        {
            _speedBonusModifier.Value = multiplier;
        }
    }

    [Command]
    private void CmdApplySpeedBoost(float multiplier)
    {
        _syncedSeriesSpeed = multiplier;
        ApplySeriesSpeed(multiplier);
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
        _timerPaused = false;
        _timer = 0f;
        RefreshSeriesBuff();
        ApplySeriesSpeed(1f);
        CmdResetSpeedBoost();
        ClearSeriesBoosts();
    }

    [Command]
    private void CmdResetSpeedBoost()
    {
        _syncedSeriesSpeed = 1f;
        ApplySeriesSpeed(1f);
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
        RefreshSeriesBoosts();
    }

    private void RemovePattern(string name)
    {
        if (string.IsNullOrEmpty(name))
            return;

        int removedCount = _availablePatterns.RemoveAll(pattern => pattern.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        
        if (removedCount > 0)
            RefreshSeriesBoosts();
    }

    #region Skill

    protected override IEnumerator CastJob()
    {
        throw new NotImplementedException();
    }

    protected override int AnimTriggerPrepare { get; }
    protected override int AnimTriggerCast { get; }
    #endregion
}
