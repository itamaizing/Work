using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class RingOfFireSkill : Skill,IFireComboParticipatingSkill
{
    [SerializeField] private RingOfFireAura _ringOfFireAura;

    private const float RingDuration = 6f;
    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("FireRing");
    
    private const float RadiusPerFullCombo = 1f;

    private float _currentComboBonus = 0f;
    
    public event Action OnRingEnabled;
    
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
        bool shouldActivate = !_ringOfFireAura.IsActive;
        
        if (shouldActivate)
        {
            OnRingEnabled?.Invoke();
            CmdActivateRingWithDuration(RingDuration);
        }
        else
        {
            CmdDeactivateRing();
        }

        yield return null;
    }
    
    [Command]
    private void CmdActivateRingWithDuration(float duration)
    {
        RpcActivateRingWithDuration(duration);
    }

    [ClientRpc]
    private void RpcActivateRingWithDuration(float duration)
    {
        if (_ringOfFireAura == null) return;
        
        _ringOfFireAura.ActivateAura(true, duration, false, this);
    }

    [Command]
    private void CmdDeactivateRing()
    {
        RpcDeactivateRing();
    }

    [ClientRpc]
    private void RpcDeactivateRing()
    {
        if (_ringOfFireAura == null) return;
        _ringOfFireAura.ActivateAura(false);
    }

    protected override void CommitUse()
    {
        UseCooldownOrCharges();
    }

    protected override void ClearData() { }

    public override void LoadTargetData(TargetInfo targetInfo) { }
    public void OnFinalComboSkill(GameObject target)
    {
        RpcApplyRadiusBonus(RadiusPerFullCombo);
    }

    public void OnTargetHasComboPoint(GameObject target, float comboPoints)
    {
        RpcApplyRadiusBonus(comboPoints);
    }
    
    [ClientRpc]
    private void RpcApplyRadiusBonus(float comboBonus)
    {
        if (_ringOfFireAura == null) return;
        _ringOfFireAura.SetRadius(comboBonus);
    }
}
