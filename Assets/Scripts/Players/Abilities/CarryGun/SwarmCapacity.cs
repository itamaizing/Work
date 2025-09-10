using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmCapacity : Skill, IPassiveSkill, ICounterSkill
{
    #region Skill
    protected override int AnimTriggerCastDelay => throw new NotImplementedException();
    protected override int AnimTriggerCast => throw new NotImplementedException();
    public override void LoadTargetData(TargetInfo targetInfo) => throw new NotImplementedException();

    protected override IEnumerator CastJob()
    {
        yield return null;
    }

    protected override void ClearData() => throw new NotImplementedException();
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) => throw new NotImplementedException();
    #endregion

    private SpawnComponent _spawnComponent;

    private void Start()
    {
        _spawnComponent = Hero.GetComponent<SpawnComponent>();
        if (_spawnComponent != null)
        {
            _spawnComponent.UnitAdded += UpdateCounter;
            _spawnComponent.UnitRemoved += UpdateCounter;

            UpdateCounter();
        }
    }

    private void OnDestroy()
    {
        if (_spawnComponent != null)
        {
            _spawnComponent.UnitAdded -= UpdateCounter;
            _spawnComponent.UnitRemoved -= UpdateCounter;
        }
    }

    private void UpdateCounter(Character _) => UpdateCounter();

    private void UpdateCounter()
    {
        if (_spawnComponent != null)
            CurrentCounter = _spawnComponent.Units.Count;
    }
}
