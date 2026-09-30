using System;
using System.Collections;
using UnityEngine;
using Mirror;
using System.Linq;
using System.Collections.Generic;

public class SwarmCapacity : Skill, IPassiveSkill, ICounterSkill
{
    #region Skill
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator CastJob() { yield return null; }
    protected override void ClearData() { }
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) { yield return null; }
    #endregion

    private float _baseCounter;

    #region SwarmTalent_8
    
    private const float SpeedBonusPercent = 1.5f;
    private const float TargetWindowDuration = 1.5f;

    private GameObject _currentTarget;
    private Coroutine _targetBoostRoutine;
    private readonly AttributeModifier _attackSpeedModifier = new AttributeModifier(SpeedBonusPercent, ModifierType.Percent);
    
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

        if (Hero != null)
        {
            Hero.DamageTracker.OnDamageTracked += OnDamageTracked;
        }
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

        if (Hero != null && Hero.DamageTracker != null)
        {
            Hero.DamageTracker.OnDamageTracked -= OnDamageTracked;
        }

        ResetBoostState();
    }

    private void UpdateCounter(Character _) => UpdateCounter();

    #region Boost speed CreatureCarryGun

    private void OnDamageTracked(Damage damage, GameObject target)
    {
        if (!isServer) return;
        if (!_isBoostSpeedSwarmDamage) return;
        if (target == null) return;
        if (damage.Type != DamageType.Physical) return;
        
        ActivateTargetSpeedBoost(_hero.gameObject, target);
    }

    [TargetRpc]
    private void ActivateTargetSpeedBoost(GameObject hero,GameObject target)
    {
        ResetBoostState();
        _currentTarget = target;

        ApplySpeedBoostToUnits();

        _targetBoostRoutine = StartCoroutine(TargetBoostTimer());
    }

    private void ApplySpeedBoostToUnits()
    {
        foreach (var unit in _swarmUnits)
        {
            if (unit == null) continue;

            AddSpeedForMinion(unit.gameObject);

            if (unit.TryGetComponent<Character>(out var unitChar) && unitChar.DamageTracker != null)
            {
                unitChar.DamageTracker.OnDamageTracked += OnUnitDamageTracked;
            }
        }
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

    private void OnUnitDamageTracked(Damage damage, GameObject unitTarget)
    {
        if (_currentTarget != null && unitTarget == _currentTarget)
        {
            ResetBoostState();
        }
    }

    private IEnumerator TargetBoostTimer()
    {
        yield return new WaitForSeconds(TargetWindowDuration);
        ResetBoostState();
    }

    private void ResetBoostState()
    {
        if (_targetBoostRoutine != null)
        {
            StopCoroutine(_targetBoostRoutine);
            _targetBoostRoutine = null;
        }

        _currentTarget = null;

        foreach (var unit in _swarmUnits)
        {
            if (unit == null) continue;

            RemoveSpeedMinion(unit.gameObject);
            
            if (unit.TryGetComponent<Character>(out var character))
            {
                if (character.DamageTracker != null)
                {
                    character.DamageTracker.OnDamageTracked -= OnUnitDamageTracked;
                }
            }
        }
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

        while (true)
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
                        PhysicAttackType = AttackRangeType.MeleeAttack,
                    };

                    CmdApplyDamageSwarm(damage, minion.gameObject);
                }
            }

            yield return delay;
        }
    }

    [Command]
    private void CmdApplyDamageSwarm(Damage damage, GameObject target)
    {
        if (target == null) return;

        var hp = target.GetComponent<Health>();
        if (hp == null) return;

        hp.TryTakeDamage(ref damage, this);
        Hero.DamageTracker.AddDamage(damage, target, isServerRequest: true);
    }
}