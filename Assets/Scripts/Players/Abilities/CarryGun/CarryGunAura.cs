using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarryGunAura : Skill
{
    [SerializeField] private SpawnComponent _spawnComponent;

    private List<MinionComponent> _swarm = new();
    private List<ScraderSpawn> _activeScraderSpawns = new();

    #region Skill
    protected override int AnimTriggerCastDelay => throw new NotImplementedException();
    protected override int AnimTriggerCast => throw new NotImplementedException();
    protected override bool IsCanCast => throw new NotImplementedException();
    public override void LoadTargetData(TargetInfo targetInfo) => throw new NotImplementedException();
    protected override IEnumerator CastJob() => throw new NotImplementedException();
    protected override void ClearData() => throw new NotImplementedException();
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) => throw new NotImplementedException();
    #endregion

    public void AddToSwarm(MinionComponent minion)
    {
        if (!_swarm.Contains(minion)) _swarm.Add(minion);
    }

    public void SubscribeScraderSpawn(ScraderSpawn scraderSpawn)
    {
        if (!_activeScraderSpawns.Contains(scraderSpawn))
        {
            _activeScraderSpawns.Add(scraderSpawn);
            scraderSpawn.Setup(_spawnComponent, this);
        }
    }

    public void UnsubscribeScraderSpawn(ScraderSpawn scraderSpawn)
    {
        if (_activeScraderSpawns.Contains(scraderSpawn))
        {
            _activeScraderSpawns.Remove(scraderSpawn);
        }
    }
}
