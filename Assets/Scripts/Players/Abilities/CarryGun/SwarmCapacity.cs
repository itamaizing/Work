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

            if (_isAddCounter) _baseCounter += 1;
            else
            {
                _baseCounter -= 1;

                if (_baseCounter < MaxCounter) _baseCounter = MaxCounter;
            }
        }
    }

    public void BoostSpeedSwarmDamage(bool value) => _isBoostSpeedSwarmDamage = value;
    public void AddCounter(bool value) => _isAddCounter = value;

    #endregion

    private SpawnComponent _spawnComponent;
    private Coroutine _overloadCheckRoutine;

    private readonly List<MoveCreature> _swarmUnits = new();

    public IReadOnlyList<MoveCreature> SwarmUnits => _swarmUnits;
    public event Action<float> CounterChanged;

    private const string DamageBoostSource = "SwarmDamageBoost";
    private const float DamageBoostMultiplier = 2.5f;
    private const float DamageBoostDuration = 1f;

    private Coroutine _damageBoostRoutine;

    private void Start()
    {
        base.Init(_skillRender, _hero);

        _spawnComponent = Hero.GetComponent<SpawnComponent>();

        _baseCounter = MaxCounter;

        if (_spawnComponent != null)
        {
            _spawnComponent.UnitAdded += UpdateCounter;
            _spawnComponent.UnitRemoved += UpdateCounter;
            UpdateCounter();
        }

        if (Hero != null && Hero.DamageTracker != null)
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
    }

    private void UpdateCounter(Character _) => UpdateCounter();

    #region Boost speed CreatureCarryGun

    private void OnDamageTracked(Damage damage, GameObject target)
    {
        if (!isServer) return;
        if (!_isBoostSpeedSwarmDamage) return;
        if (damage.Type != DamageType.Physical) return;

        ActivateDamageBoost();
    }

    private void ActivateDamageBoost()
    {
        if (_damageBoostRoutine != null)
            StopCoroutine(_damageBoostRoutine);

        ApplyDamageBoost();

        _damageBoostRoutine = StartCoroutine(DamageBoostTimer());
    }

    private IEnumerator DamageBoostTimer()
    {
        yield return new WaitForSeconds(DamageBoostDuration);

        RemoveDamageBoost();
        _damageBoostRoutine = null;
    }

    private void RemoveDamageBoost()
    {
        foreach (var unit in _swarmUnits)
        {
            if (unit == null) continue;

            unit.RemoveSpeedModifier(DamageBoostSource);
        }
    }

    private void ApplyDamageBoost()
    {
        foreach (var unit in _swarmUnits)
        {
            if (unit == null) continue;

            unit.SetSpeedModifier(DamageBoostSource, DamageBoostMultiplier);
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

            if (unit.TryGetComponent(out MoveCreature carryGun)) _swarmUnits.Add(carryGun);
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
                .Where(unit => unit != null && !unit.TryGetComponent<MucusAutoGrowth>(out _))
                .Select(unit => unit.GetComponent<MinionComponent>()).Where(minion => minion != null).Sum(minion => minion.CostCall);

            if (CurrentCounter > _baseCounter)
            {
                float overloadCount = realCost - _baseCounter;
                float percentDamage = overloadCount * 0.05f;

                foreach (var minion in _spawnComponent.Units)
                {
                    if (minion == null || minion.IsDead) continue;
                    if (minion.TryGetComponent<MucusAutoGrowth>(out _)) continue;
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