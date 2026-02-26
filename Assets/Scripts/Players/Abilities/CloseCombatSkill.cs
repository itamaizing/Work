using System;
using System.Collections;
using UnityEngine;

public abstract class CloseCombatSkill : Skill
{
	//TEST CLASS FOR OVERRIDE PREPARE

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget();
            }
            yield return null;
        }
        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        targetDataSavedCallback(targetInfo);
    }
}
