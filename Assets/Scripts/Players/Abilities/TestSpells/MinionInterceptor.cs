using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionInterceptor : Skill
{
    //private MinionComponent _target;

    protected override bool IsCanCast => true;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    public override void LoadTargetData(TargetInfo targetInfo)
    {

        Targeting.SetTarget((ITargetable)(MinionComponent)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        //CmdIntercept(_target.gameObject);
        CmdIntercept(Targeting.GetTarget().Character.gameObject);
        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTarget().Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(true);
                //var temp = GetRaycastTarget();

                if (Targeting.GetTarget().Character is MinionComponent minion)
                {
                    //_target = minion;
                }
                else
                {
                    Targeting.ClearTarget();
                }
            }
            yield return null;
        }
        TargetInfo targetInfo = new();
        targetInfo.GetTargets().Add(Targeting.GetTarget().Character);
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
