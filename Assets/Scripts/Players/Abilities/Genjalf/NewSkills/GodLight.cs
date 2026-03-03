using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class GodLight : Skill
{
    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => Animator.StringToHash("GodLight");
    
    public void AnimCastGodLight()
    {
        AnimStartCastCoroutine();
    }

    public void AnimGodLightEnd()
    {
        AnimCastEnded();
    }
    
    public override void LoadTargetData(TargetInfo targetInfo) { }

    protected override IEnumerator CastJob()
    {
        CmdAddState();

        yield return null;
    }

    [Command]
    private void CmdAddState()
    {
        _hero.CharacterState.AddState(States.GodLight,-1,0,_hero.gameObject,nameof(GodLight));
    }

    protected override void ClearData() { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) { yield return null; }
}
