using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class MergeWithDarknessSkill : Skill
{
    [SerializeField] private float _duration = 4f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;
    public override bool IsPayCostStartCooldown => false;

    private bool _isActive = false;
    
    private Coroutine _durationJob;

    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override void ClearData() { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(_hero);
        targetDataSavedCallback(targetInfo);
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        if (_isActive)
        {
            Deactivate();
            yield break;
        }

        _isActive = true;
        CmdApplyState();

        _durationJob = StartCoroutine(DurationJob());

        yield return null;
    }

    private IEnumerator DurationJob()
    {
        yield return new WaitForSeconds(_duration);

        _isActive = false;
        _durationJob = null;

        Cooldown.Start();
        IncreaseSetCooldown(CooldownTime);
    }

    private void Deactivate()
    {
        if (!_isActive) return;
        _isActive = false;

        if (_durationJob != null)
        {
            StopCoroutine(_durationJob);
            _durationJob = null;
        }

        CmdRemoveState();

        Cooldown.Start();
        IncreaseSetCooldown(CooldownTime);
    }

    [Command]
    private void CmdApplyState()
    {
        Hero.CharacterState.AddState(
            States.MergeDark,
            _duration,
            0,
            Hero.gameObject,
            name
        );
        
        if (!Hero.CharacterState.CheckForState(States.ReversePolarity))
        {
            Hero.CharacterState.AddState(States.ReversePolarity,-1f, 0, transform.gameObject, Name);
        }
        
        RpcSwitchToDarkMode();
    }

    [Command]
    private void CmdRemoveState()
    {
        Hero.CharacterState.RemoveState(States.MergeDark);
        
        if (Hero.CharacterState.CheckForState(States.ReversePolarity))
        {
            Hero.CharacterState.RemoveState(States.ReversePolarity);
        }
        
        RpcSwitchToLightMode();
    }
    
    [ClientRpc]
    private void RpcSwitchToDarkMode()
    {
        foreach (var skill in Hero.Abilities.Abilities)
            if (skill is IPolaritySwitchable s && s.IsLightMode)
                s.SwitchMode();
    }

    [ClientRpc]
    private void RpcSwitchToLightMode()
    {
        foreach (var skill in Hero.Abilities.Abilities)
            if (skill is IPolaritySwitchable s && !s.IsLightMode)
                s.SwitchMode();
    }
}
