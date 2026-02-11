using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class CoolingActive : Skill
{
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => CheckCanCast();

    private float _clickRadius = 0.5f;
    
    private GameObject _shield;
    
    private Vector3 _clickPoint = Vector3.zero;

    private bool CheckCanCast()
    {
        return true;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        while (_clickPoint == Vector3.zero)
        {
            if (GetMouseButton)
            {
                _clickPoint = GetMousePoint();
            }
            yield return null;
        }
        targetInfo.AddTarget(_hero);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        
        CmdAddAura();

        yield return null;
    }
    
    [Command]
    private void CmdAddAura()
    {
        _hero.CharacterState.AddState(States.CoolingAura, 5f, 0, Hero.gameObject, name);
    }
    
    protected override void ClearData()
    {
        ClearTarget();
        Hero.Move.StopLookAt();
        _clickPoint = Vector3.zero;
    }
}
