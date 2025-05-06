using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionInterceptor : Skill
{
    private MinionComponent _target;

    protected override bool IsCanCast => true;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _target = (MinionComponent)targetInfo.Targets[0];
    }

    protected override IEnumerator CastJob()
    {
        CmdIntercept(_target.gameObject);
        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                var temp = GetRaycastTarget();

                if(temp is MinionComponent minion)
                    _target = minion;
            }
            yield return null;
        }
        TargetInfo targetInfo = new();
        targetInfo.Targets.Add(_target);
        callbackDataSaved?.Invoke(targetInfo);
    }

    [Command]
    private void CmdIntercept(GameObject minion)
    {
        minion.GetComponent<MinionComponent>().SetAuthority(connectionToClient);
        
        if(Hero is HeroComponent hero)
        {
            hero.SpawnComponent.AddUnit(minion.GetComponent<MinionComponent>());
        }
    }
}
