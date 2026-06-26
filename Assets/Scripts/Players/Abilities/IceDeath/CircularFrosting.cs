using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircularFrosting : Skill,IEnergyDamagable,IComboSeriesParticipatingSkill
{
    [SerializeField] private ParticleSystemController _particleSystem;

    private float _delayDuration;
    private float _delayStartTime;
    private bool _delayActive;

    private float _remainingDelay;
    [SyncVar] private bool _wasInterruptedInDelay;

    public float RemainingDelay => _remainingDelay;
    public bool WasInterruptedInDelay => _wasInterruptedInDelay;

    private List<Character> _enemies = new();

    private float _baseDuration = 2f;
    private float _duration = 2f;
    private float _runeCost = 3f; 
    private float _maxEnergyForBonus = 30f;
    private float _energyPerSecondStep = 10f;
    private float _currentRuneCost;
    private float _currentEnergyCost;
    private Energy _energy;
    private RuneComponent _rune;
    private bool _talentFrostingFrozen;

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private void OnEnable()
    {
        CastDeleyStarted += OnCastDelayStarted;
        CastDeleyEnded += OnCastDelayEnded;
        OnSkillCanceled += OnSkillCanceledHandler;
    }

    private void OnDisable()
    {
        CastDeleyStarted -= OnCastDelayStarted;
        CastDeleyEnded -= OnCastDelayEnded;
        OnSkillCanceled -= OnSkillCanceledHandler;
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        if (_energy == null) _energy = (Energy)hero.Resources[ResourceType.Energy];
        if(_rune == null) _rune = (RuneComponent)hero.Resources[ResourceType.Rune];
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);
        callbackDataSaved(targetInfo);
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        if (_energy == null) yield break;
        if (!IsCasting) yield break;

        if (!Cost.TryPaySingle(_runeCost, ResourceType.Rune, shouldModify: false))
        {
            TryCancel(true);
            yield break;
        }

        _currentRuneCost = _runeCost;

        FindEnemies();
        ExplosionFrosting();

        if (_isSeriesComplete)
        {
            OnSeriesDamaged?.Invoke(null, this);
            _isSeriesComplete = false;
        }

        _particleSystem?.Play();

        yield return null;
        _currentEnergyCost = 0;
        _currentRuneCost = 0;
    }

    private void FindEnemies()
    {
        _enemies.Clear();

        Collider[] hits = Physics.OverlapSphere(transform.position, AreaInfo.Radius);

        foreach (var col in hits)
        {
            Character character = col.GetComponent<Character>();
            if (character != null && character != Hero && !_enemies.Contains(character)) _enemies.Add(character);
        }
    }

    private void ExplosionFrosting()
    {
        _currentEnergyCost = Mathf.Min(_energy.CurrentValue, _maxEnergyForBonus);

        int bonusSteps = Mathf.FloorToInt(_currentEnergyCost / _energyPerSecondStep);
        _duration = _baseDuration + bonusSteps;

        if (_currentEnergyCost > 0f)
            _energy.CmdUse(_currentEnergyCost);

        foreach (Character target in _enemies)
        {
            if (target == null) continue;
            CmdApplyFrosting(target, _currentEnergyCost, _duration);
        }
    }

    [Command]
    private void CmdApplyFrosting(Character target, float usedEnergy, float duration)
    {
        if (target == null) return;

        var frostEnergy = Hero.Abilities.GetSkill<FrostEnergy>();
        
        target.CharacterState.AddState(States.Frosting, duration, 0, Hero.gameObject, name);
        frostEnergy?.ApplyFrostEnergyStateBonus(target, States.Frosting, this);
    }

    public void SetTalentFrostingFrozen(bool value)
    {
        _talentFrostingFrozen = value;
    }

    protected override void ClearData()
    {
        _enemies.Clear();
    }

    private void OnCastDelayStarted(float duration)
    {
        _delayDuration = duration;
        _delayStartTime = Time.time;
        _delayActive = true;

        _wasInterruptedInDelay = true;
        _remainingDelay = 0f;
    }

    private void OnCastDelayEnded()
    {
        _delayActive = false;
    }

    private void OnSkillCanceledHandler()
    {
        if (!_delayActive) return;

        float elapsed = Time.time - _delayStartTime;
        _remainingDelay = Mathf.Max(0f, _delayDuration - elapsed);

        _wasInterruptedInDelay = true;
        _delayActive = false;
    }

    public void ConsumeInterruptedDelay()
    {
        _wasInterruptedInDelay = false;
        _remainingDelay = 0f;
    }

    [ClientRpc]
    public void PayEnergyOnInterruptedDelay()
    {
        if (!_wasInterruptedInDelay) return;

        if (_energy == null) _energy = (Energy)Hero.Resources[ResourceType.Energy];

        if (_energy == null) return;

        float energyToUse = Mathf.Min(_energy.CurrentValue, 30f);

        if (energyToUse > 0f) _energy.CmdUse(energyToUse);

        _wasInterruptedInDelay = false;
    }

    #region Series

    public bool IsStreamSkill { get; }
    public bool IsFrostEnergyApplied => true;

    public event IComboSeriesParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplySeriesDamage;
    public event Action<GameObject, Skill> OnSeriesDamaged;
    public float EnergyCostOnHit => _currentEnergyCost;
    public float RuneCostOnHit => _currentRuneCost;
    public bool IsTicking { get; }

    private bool _isSeriesComplete;

    public void OnSeriesHit(int hitCountInCurrentSeries, Character target) { }

    public void OnSeriesCompleted(Character target, int totalHits, float totalEnergySpent)
    {

    }

    public void OnSeriesBroken(Character target)
    {
        _isSeriesComplete = false;
    }

    public void OnSeriesPotentialFinal(Skill skill, bool isPotentialFinal)
    {
        if (isPotentialFinal && _energy.CurrentValue > 5 && _rune.CurrentValue >= _runeCost)
        {
            Hero.Abilities.SetNextSkillNoCast();
            _isSeriesComplete = true;
        }
    }

    #endregion
}