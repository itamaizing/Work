using System;
using System.Collections;
using UnityEngine;

class ExampleTargetedAbility : Skill
{
    protected override int AnimTriggerPrepare => throw new NotImplementedException();

    protected override int AnimTriggerCast => throw new NotImplementedException();

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        throw new NotImplementedException();
    }

    protected override IEnumerator TargetingJob(Action<TargetInfo> targetDataSavedCallback)
    {
        return base.TargetingJob(targetDataSavedCallback);
    }

    protected override IEnumerator CastJob()
    {
        throw new NotImplementedException();
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        AnimCastEnded();
    }
}