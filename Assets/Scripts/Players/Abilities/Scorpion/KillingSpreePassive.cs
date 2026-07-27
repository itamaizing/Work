using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class KillingSpreePassive : Skill
{
    private float _duration = 4f;

    protected override bool IsCanCast => CanCast();
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private bool CanCast()
    {
        //if (_hero.CharacterState.CheckForState(States.KillingSpree)) return false;
        return true;
    }
    
    private void EnableKillingSpree()
    {
        CmdAddKillingSpree();
    }

    [Command]
    private void CmdAddKillingSpree()
    {
        _hero.CharacterState.AddState(States.KillingSpree, _duration, 0, Schools.Physical, _hero.gameObject, nameof(KillingSpreeState));
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo info = new TargetInfo();
        info.AddTarget(Hero);
        callbackDataSaved(info);
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        EnableKillingSpree();
        //CommitUse();
        yield return null;
    }
}
