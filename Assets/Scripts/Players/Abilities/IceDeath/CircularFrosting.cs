using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircularFrosting : Skill,IEnergyDamagable
{
    [SerializeField] private Character _player;
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

    private Energy _energy;
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

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);
        callbackDataSaved(targetInfo);
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        if (_energy == null) _energy = (Energy)Hero.Resources[ResourceType.Energy];
        if (_energy == null) yield break;
        if (!IsCasting) yield break;

        if (!Cost.TryPaySingle(_runeCost, ResourceType.Rune, shouldModify: false))
        {
            TryCancel(true);
            yield break;
        }
        
        FindEnemies();
        ExplosionFrosting();

        _particleSystem?.Play();

        yield return null;
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
        float usedEnergy = Mathf.Min(_energy.CurrentValue, _maxEnergyForBonus);

        int bonusSteps = Mathf.FloorToInt(usedEnergy / _energyPerSecondStep);
        _duration = _baseDuration + bonusSteps;

        if (usedEnergy > 0f)
            _energy.CmdUse(usedEnergy);

        foreach (Character target in _enemies)
        {
            if (target == null) continue;
            CmdApplyFrosting(target, usedEnergy, _duration);
        }
    }

    [Command]
    private void CmdApplyFrosting(Character target, float usedEnergy, float duration)
    {
        if (target == null) return;

        var frostEnergy = Hero.Abilities.GetSkill<FrostEnergy>();

        /*if (_talentFrostingFrozen && target.CharacterState.CheckForState(States.Frosting))
        {
            target.CharacterState.AddState(States.Frozen, duration, 0, Hero.gameObject, name);
            frostEnergy?.ApplyFrostEnergyStateBonus(target, States.Frozen, this);
        }*/

        Debug.LogError("TryFrosting: "+duration);
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

    public bool IsStreamSkill { get; }
    public bool IsFrostEnergyApplied => true;
}