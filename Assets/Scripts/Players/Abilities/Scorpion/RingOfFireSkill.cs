using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class RingOfFireSkill : Skill
{
    [SerializeField] private RingOfFireAura _ringOfFireAura;

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("FireRing");
    
    public void AnimCastRing()
    {
        AnimStartCastCoroutine();
    }

    public void AnimRingEnd()
    {
        AnimCastEnded();
    }   

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        callbackDataSaved(new TargetInfo());
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        bool newState = !_ringOfFireAura.IsActive;
        CmdToggleRing(newState);
        yield return null;
    }

    [Command]
    private void CmdToggleRing(bool active)
    {
        RpcToggleRing(active);
    }

    [ClientRpc]
    private void RpcToggleRing(bool active)
    {
        if (_ringOfFireAura == null) return;

        if (active)
            _ringOfFireAura.ActivateAura(true,-1,false,this);
        else
            _ringOfFireAura.ActivateAura(false);
    }

    protected override void ClearData() { }

    public override void LoadTargetData(TargetInfo targetInfo) { }
}
