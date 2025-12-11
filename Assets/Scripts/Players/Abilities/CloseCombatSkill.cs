using System;
using System.Collections;
using UnityEngine;

public abstract class CloseCombatSkill : Skill
{
	//TEST CLASS FOR OVERRIDE PREPARE

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (GetTempTargetCharacter() == null)
        {
            if (GetMouseButton)
            {
                FindTarget();
            }
            yield return null;
        }
        SetTargetCharacter(GetTempTargetCharacter());
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(GetTargetCharacter());
        targetDataSavedCallback(targetInfo);
    }
}
