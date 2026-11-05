using System;
using System.Collections;
using UnityEngine;
using Mirror;
using System.Linq;
using System.Collections.Generic;

public class SwarmCapacity : Skill, IPassiveSkill, ICounterSkill
{
    #region Skill
    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => 0;
    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator CastJob() { yield return null; }
    protected override void ClearData() { }
    protected override IEnumerator TargetingJob(Action<TargetInfo> targetDataSavedCallback) { yield return null; }
    #endregion

    private float _baseCounter;

    #region SwarmTalent_8

    private uint _boostVersion;
    private double _boostWindowEnd;
    
    private const float SpeedBonusPercent = 1.5f;
    private const float TargetWindowDuration = 1.5f;

    private GameObject _currentTarget;
    private Coroutine _targetBoostRoutine;
    private readonly AttributeModifier _attackSpeedModifier = new AttributeModifier(SpeedBonusPercent, ModifierType.Percent);
    
    private readonly HashSet<MinionComponent> _eligibleBoostUnits = new();

    private readonly Dictionary<MinionComponent, Skill> _boostedCasts = new();

    private readonly List<(Skill skill, Action started, Action ended)> _boostSubscriptions = new();
    
    private readonly Dictionary<Skill, Character> _heroCastTargets = new();
    
    #endregion
    
    #region Talent

    private bool _isBoostSpeedSwarmDamage = false;
    private bool _isAddCounter = false;

    public bool IsAddCharges
    {
        get => _isAddCounter;
        set
        {
            if (_isAddCounter == value) return;

            _isAddCounter = value;

            if (_isAddCounter)
            {
                MaxCounter += 1;
            }
            else
            {
                MaxCounter -= 1;
            }
            _baseCounter = MaxCounter;

            UpdateCounter();
        }
    }

    public void BoostSpeedSwarmDamage(bool value) => _isBoostSpeedSwarmDamage = value;

    #endregion

    private SpawnComponent _spawnComponent;
    private Coroutine _overloadCheckRoutine;

    private readonly List<MinionComponent> _swarmUnits = new();

    public IReadOnlyList<MinionComponent> SwarmUnits => _swarmUnits;
    public event Action<float> CounterChanged;

    private Coroutine _damageBoostRoutine;

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        _spawnComponent = Hero.GetComponent<SpawnComponent>();

        _baseCounter = MaxCounter;

        if (_spawnComponent != null)
        {
            _spawnComponent.UnitAdded += UpdateCounter;
            _spawnComponent.UnitRemoved += UpdateCounter;
            UpdateCounter();
        }

        Hero.Abilities.OnSkillPreparedSuccessfully += RememberHeroCastTarget;
        Hero.Abilities.SkillCastEnded += OnHeroCastEnded;
    }

    private void OnDisable()
    {
        if (_spawnComponent != null)
        {
            _spawnComponent.UnitAdded -= UpdateCounter;
            _spawnComponent.UnitRemoved -= UpdateCounter;
        }

        if (_overloadCheckRoutine != null)
        {
            StopCoroutine(_overloadCheckRoutine);
            _overloadCheckRoutine = null;
        }

        if (Hero != null && Hero.Abilities != null)
        {
            Hero.Abilities.OnSkillPreparedSuccessfully -= RememberHeroCastTarget;
            Hero.Abilities.SkillCastEnded -= OnHeroCastEnded;
        }

        _heroCastTargets.Clear();

        ResetBoostStateLocal();
    }

    private void UpdateCounter(Character _) => UpdateCounter();

    #region Boost speed CreatureCarryGun

    private void RememberHeroCastTarget(Skill skill)
    {
        if (!isOwned || skill == null)
            return;

        _heroCastTargets.Remove(skill);

        if (!_isBoostSpeedSwarmDamage || skill.Info.AbilityForm == AbilityForm.Magic)
            return;

        var target = skill.Targeting.GetTarget()?.Character;

        if (target != null && Hero.RelationTo(target) == CharacterRelation.Enemy)
        {
            _heroCastTargets[skill] = target;
        }
    }

    private void OnHeroCastEnded(Skill skill)
    {
        if (!isOwned || skill == null)
            return;

        if (!_heroCastTargets.TryGetValue(skill, out var target))
            return;
        
        _heroCastTargets.Remove(skill);

        if (skill.LastTransitionCanceled)
            return;

        if (!_isBoostSpeedSwarmDamage || target == null || target.IsDead)
            return;

        CmdActivateBoostAfterHeroCast(skill, target);
    }

    [Command]
    private void CmdActivateBoostAfterHeroCast(Skill skill, Character target)
    {
        if (!_isBoostSpeedSwarmDamage)
            return;

        if (skill.Info.AbilityForm == AbilityForm.Magic || !Hero.Abilities.Skills.Contains(skill))
            return;

        if (target == null || target.IsDead)
            return;

        if (Hero.RelationTo(target) != CharacterRelation.Enemy)
            return;

        ActivateTargetSpeedBoost(target.gameObject);
    }

    [Server]
    private void ActivateTargetSpeedBoost(GameObject target)
    {
        uint version = ++_boostVersion;
        double endTime = NetworkTime.time + TargetWindowDuration;

        StartBoostWindow(target, version, endTime);
        TargetActivateSpeedBoost(target, version, endTime);
    }
    
    [TargetRpc]
    private void TargetActivateSpeedBoost(GameObject target, uint version, double endTime)
    {
        if (isServer) return;

        StartBoostWindow(target, version, endTime);
    }

    private void StartBoostWindow(GameObject target, uint version, double endTime)
    {
        ResetBoostStateLocal();

        _boostVersion = version;
        _currentTarget = target;
        _boostWindowEnd = endTime;

        ApplySpeedBoostToUnits();

        _targetBoostRoutine = StartCoroutine(TargetBoostTimer());
    }

    private void ApplySpeedBoostToUnits()
    {
        foreach (var unit in _swarmUnits)
        {
            if (unit == null || unit.IsDead || unit.Abilities == null) continue;

            if (!_eligibleBoostUnits.Add(unit)) continue;

            AddSpeedForMinion(unit.gameObject);

            if (!isOwned) continue;

            foreach (var skill in unit.Abilities.Skills)
            {
                if (skill == null) continue;

                var capturedUnit = unit;
                var capturedSkill = skill;
                uint version = _boostVersion;

                Action started = () => OnBoostCastStarted(capturedUnit, capturedSkill, version);

                Action ended = () => OnBoostCastEnded(capturedUnit, capturedSkill, version);

                skill.CastStarted += started;
                skill.CastEnded += ended;

                _boostSubscriptions.Add((skill, started, ended));
            }
        }
    }

    private void OnBoostCastStarted(MinionComponent unit, Skill skill, uint version)
    {
        if (version != _boostVersion) return;
        if (!_eligibleBoostUnits.Contains(unit)) return;
        if (_boostedCasts.ContainsKey(unit)) return;
        if (NetworkTime.time > _boostWindowEnd) return;

        var target = skill.Targeting.GetTarget()?.Character;
        if (target == null || target.gameObject != _currentTarget)
            return;

        _boostedCasts.Add(unit, skill);

        if (!isServer)
        {
            CmdBoostCastStarted(unit, skill, version, NetworkTime.time);
        }
    }

    [Command]
    private void CmdBoostCastStarted(MinionComponent unit, Skill skill, uint version, double startedAt)
    {
        if (version != _boostVersion) return;
        if (unit == null || skill == null || skill.Hero != unit) return;
        if (!_eligibleBoostUnits.Contains(unit)) return;
        if (_boostedCasts.ContainsKey(unit)) return;

        if (startedAt < _boostWindowEnd - TargetWindowDuration || startedAt > _boostWindowEnd || startedAt > NetworkTime.time + 0.1d)
            return;

        _boostedCasts.Add(unit, skill);

        AddSpeedForMinion(unit.gameObject);
    }

    private void OnBoostCastEnded(MinionComponent unit, Skill skill, uint version)
    {
        if (version != _boostVersion) return;

        if (!_boostedCasts.TryGetValue(unit, out var boostedSkill) || boostedSkill != skill) 
            return;

        RemoveUnitBoost(unit);

        if (!isServer)
            CmdBoostCastEnded(unit, skill, version);
    }

    [Command]
    private void CmdBoostCastEnded(MinionComponent unit, Skill skill, uint version)
    {
        if (version != _boostVersion || unit == null) return;

        if (!_boostedCasts.TryGetValue(unit, out var boostedSkill) || boostedSkill != skill)
            return;

        RemoveUnitBoost(unit);
    }

    private void RemoveUnitBoost(MinionComponent unit)
    {
        _boostedCasts.Remove(unit);
        _eligibleBoostUnits.Remove(unit);

        if (unit != null)
            RemoveSpeedMinion(unit.gameObject);
    }

    private void AddSpeedForMinion(GameObject minion)
    {
        if (minion == null) return;

        if (minion.TryGetComponent<Character>(out var character))
        {
            var castSpeedAttr = character.AttributeSystem[CharacterAttributeName.CastSpeed];
            if (castSpeedAttr != null && !castSpeedAttr.Modifiers.Contains(_attackSpeedModifier))
            {
                castSpeedAttr.AddModifier(_attackSpeedModifier);
            }
        }
    }
    
    private void RemoveSpeedMinion(GameObject minion)
    {
        if (minion == null) return;

        if (minion.TryGetComponent<Character>(out var character))
        {
            var castSpeedAttr = character.AttributeSystem[CharacterAttributeName.CastSpeed];
            if (castSpeedAttr != null)
            {
                castSpeedAttr.RemoveModifier(_attackSpeedModifier);
            }
        }
    }

    private IEnumerator TargetBoostTimer()
    {
        while (NetworkTime.time < _boostWindowEnd)
            yield return null;

        _targetBoostRoutine = null;

        foreach (var unit in _eligibleBoostUnits)
        {
            if (unit == null) continue;

            if (_boostedCasts.ContainsKey(unit)) continue;

            RemoveSpeedMinion(unit.gameObject);
        }
    }

    private void ResetBoostStateLocal()
    {
        if (_targetBoostRoutine != null)
        {
            StopCoroutine(_targetBoostRoutine);
            _targetBoostRoutine = null;
        }

        foreach (var subscription in _boostSubscriptions)
        {
            if (subscription.skill == null)
                continue;

            subscription.skill.CastStarted -= subscription.started;
            subscription.skill.CastEnded -= subscription.ended;
        }

        _boostSubscriptions.Clear();

        foreach (var unit in _eligibleBoostUnits)
        {
            if (unit != null)
                RemoveSpeedMinion(unit.gameObject);
        }

        _eligibleBoostUnits.Clear();
        _boostedCasts.Clear();

        _currentTarget = null;
        _boostWindowEnd = 0d;
    }

    #endregion

    private void UpdateCounter()
    {
        if (_spawnComponent == null) return;

        float totalCost = 0f;

        _swarmUnits.Clear();

        foreach (var unit in _spawnComponent.Units)
        {
            if (unit == null) continue;

            var minion = unit.GetComponent<MinionComponent>();
            if (minion != null) totalCost += minion.CostCall;

            _swarmUnits.Add(minion);
        }

        CurrentCounter = Mathf.RoundToInt(totalCost);

        CounterChanged?.Invoke(CurrentCounter);

        HandleOverload();
    }

    private void HandleOverload()
    {
        if (!isServer)
            return;
        
        if (CurrentCounter > _baseCounter)
        {
            if (_overloadCheckRoutine == null)
            {
                _overloadCheckRoutine = StartCoroutine(CheckOverloadRoutine());
            }
        }
        else
        {
            if (_overloadCheckRoutine != null)
            {
                StopCoroutine(_overloadCheckRoutine);
                _overloadCheckRoutine = null;
            }
        }
    }

    private IEnumerator CheckOverloadRoutine()
    {
        WaitForSeconds delay = new WaitForSeconds(1f);

        while (isServer)
        {
            float realCost = _spawnComponent.Units
                .Where(unit => unit != null && !unit.TryGetComponent<MucusArea>(out _))
                .Select(unit => unit.GetComponent<MinionComponent>())
                .Where(minion => minion != null)
                .Sum(minion => minion.CostCall);

            if (CurrentCounter > _baseCounter)
            {
                float overloadCount = realCost - _baseCounter;
                float percentDamage = overloadCount * 0.05f;

                foreach (var minion in _spawnComponent.Units)
                {
                    if (minion == null || minion.IsDead) continue;
                    if (minion.TryGetComponent<MucusArea>(out _)) continue;
                    if (minion.TryGetComponent<CreatureSpawn>(out _)) continue;

                    float damageValue = minion.Health.MaxValue * percentDamage;

                    Damage damage = new Damage
                    {
                        Value = damageValue,
                        Type = DamageType.None,
                        AttackRange = AttackRangeType.MeleeAttack,
                    };

                    ApplyDamageSwarmServer(damage, minion.gameObject);
                }
            }

            yield return delay;
        }
    }

    [Server]
    private void ApplyDamageSwarmServer(Damage damage, GameObject target)
    {
        if (target == null) return;

        var hp = target.GetComponent<Health>();
        if (hp == null) return;

        hp.TryTakeDamage(ref damage, this);

        Hero.DamageTracker.AddDamage(damage, target, isServerRequest: true);
    }
}