using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmSpeedAura : Skill
{
    [SerializeField] private SwarmCapacity _swarmCapacity;

    private float _currentMultiplier = 1f;
    private bool _isBuffActive = false;
    private Coroutine _buffRoutine;

    private const float BaseBonus = 0.30f;
    private const float PerUnitBonus = 0.05f;
    private const float Duration = 5f;
    private const string SwarmSpeedSource = "SwarmSpeedAura";

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    private void Start()
    {
        if (_swarmCapacity == null) return;

        _swarmCapacity.CounterChanged += OnCounterChanged;

        if (Hero != null && Hero.TryGetComponent(out SpawnComponent spawn))
        {
            spawn.UnitAdded += OnUnitChanged;
            spawn.UnitRemoved += OnUnitChanged;
        }
    }

    private void OnDisable()
    {
        if (_swarmCapacity != null)
            _swarmCapacity.CounterChanged -= OnCounterChanged;

        if (Hero != null && Hero.TryGetComponent(out SpawnComponent spawn))
        {
            spawn.UnitAdded -= OnUnitChanged;
            spawn.UnitRemoved -= OnUnitChanged;
        }

        if (_buffRoutine != null) StopCoroutine(_buffRoutine);

        RemoveAllBuffs();
    }

    protected override IEnumerator CastJob()
    {
        ActivateBuff();
        yield break;
    }

    private void ActivateBuff()
    {
        if (_buffRoutine != null)
        {
            StopCoroutine(_buffRoutine);
        }

        _isBuffActive = true;

        RecalculateSpeed();

        _buffRoutine = StartCoroutine(BuffTimer());
    }

    private IEnumerator BuffTimer()
    {
        yield return new WaitForSeconds(Duration);

        _isBuffActive = false;
        RemoveAllBuffs();
        _buffRoutine = null;
    }

    private void OnCounterChanged(float value)
    {
        Disactive = value <= 0;

        if (_isBuffActive)
            RecalculateSpeed();
    }

    private void OnUnitChanged(Character _)
    {
        if (_isBuffActive)
            RecalculateSpeed();
    }

    private void RecalculateSpeed()
    {
        if (_swarmCapacity == null)
            return;

        float counter = _swarmCapacity.CurrentCounter;

        float newMultiplier = counter <= 0 ? 1f : 1f + BaseBonus + (counter * PerUnitBonus);

        foreach (var unit in _swarmCapacity.SwarmUnits)
        {
            if (unit == null) continue;
            unit.RemoveSpeedModifier(SwarmSpeedSource);
        }

        if (counter > 0)
        {
            foreach (var unit in _swarmCapacity.SwarmUnits)
            {
                if (unit == null) continue;
                unit.SetSpeedModifier(SwarmSpeedSource, newMultiplier);
            }
        }

        _currentMultiplier = newMultiplier;
    }

    private void RemoveAllBuffs()
    {
        if (_swarmCapacity == null) return;

        foreach (var unit in _swarmCapacity.SwarmUnits)
        {
            if (unit == null) continue;
            unit.RemoveSpeedModifier(SwarmSpeedSource);
        }

        _currentMultiplier = 1f;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.GetTargets().Contains(Hero)) return;
        targetInfo.AddTarget(Hero);
    }

    protected override void ClearData() { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);
        callbackDataSaved(targetInfo);
        yield break;
    }
}