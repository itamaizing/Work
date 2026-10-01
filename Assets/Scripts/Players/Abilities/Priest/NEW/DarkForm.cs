using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class DarkForm : Skill
{
    [SerializeField] private float _manaCost = 20f;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    private bool _isActive = false;
    private Coroutine _activeJob;

    public bool IsActive => _isActive;

    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override void ClearData() { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(_hero);
        callbackDataSaved(targetInfo);
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        if (_isActive)
        {
            Deactivate();
            yield break;
        }

        var mana = Hero.Resources[ResourceType.Mana];
        if (mana.CurrentValue < _manaCost) yield break;
        mana.CmdUse(_manaCost);

        _isActive  = true;
        CmdApplyState();

        yield return null;
    }

    public void Deactivate()
    {
        if (!_isActive) return;
        _isActive = false;

        if (_activeJob != null)
        {
            StopCoroutine(_activeJob);
            _activeJob = null;
        }

        CmdRemoveState();
    }

    [Command] 
    private void CmdApplyState()
    {
        Hero.CharacterState.AddState(States.DarkFormState, -1f, 0, Hero.gameObject, name);

        if (!Hero.CharacterState.CheckForState(States.ReversePolarity))
        {
            Hero.CharacterState.AddState(States.ReversePolarity,-1f, 0, transform.gameObject, Name);
        }
        
        RpcSwitchToDarkMode();
    }

    [Command] 
    private void CmdRemoveState()
    {
        Hero.CharacterState.RemoveState(States.DarkFormState);
        
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
