using System;
using System.Collections;
using UnityEngine;

public class SummoningSwarm : Skill
{
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    private const int ChargesToAdd = 3;

    private int _chargesSwarm;

    public int ChargesSwarm => _chargesSwarm;

    private void OnEnable()
    {
        CooldownEnded += SwarmChargesNull;
    }

    private void OnDisable()
    {
        CooldownEnded -= SwarmChargesNull;
    }

    protected override IEnumerator CastJob()
    {
        SetSwarmCharges(ChargesToAdd);
        yield return null;
    }

    private void SetSwarmCharges(int value)
    {
        _chargesSwarm = value;
        CurrentCharge(_chargesSwarm);
    }

    private void SwarmChargesNull()
    {
        SetSwarmCharges(0);
    }    

    public void UseSwarmCharges(int value)
    {
        _chargesSwarm -= value;
        CurrentCharge(_chargesSwarm);
    }

    protected override void ClearData()
    {

    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.GetTargets().Contains(Hero)) return;

        targetInfo.AddTarget(Hero);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);

        callbackDataSaved(targetInfo);
        yield break;
    }
}