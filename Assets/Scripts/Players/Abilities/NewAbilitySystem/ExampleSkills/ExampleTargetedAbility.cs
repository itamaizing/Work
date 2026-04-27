using System;
using System.Collections;
using UnityEngine;

class ExampleTargetedAbility : Skill
{
    protected override int AnimTriggerCastDelay => throw new NotImplementedException();

    protected override int AnimTriggerCast => throw new NotImplementedException();

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        throw new NotImplementedException();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        return base.PrepareJob(targetDataSavedCallback);
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