using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class FrostEnergy : Skill
{
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.GetTargets().Contains(Hero)) return;

        targetInfo.AddTarget(Hero);
    }

    protected override void ClearData() { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);
        callbackDataSaved(targetInfo);

        yield break;
    }

    protected override IEnumerator CastJob()
    {
        if (Hero == null || Hero.CharacterState == null) yield break;

        CmdToggleFrostEnergy(Hero.gameObject);

        yield break;
    }

    [Command]
    private void CmdToggleFrostEnergy(GameObject targetObj)
    {
        if (targetObj == null) return;

        Character character = targetObj.GetComponent<Character>();
        if (character == null || character.CharacterState == null) return;

        if (character.CharacterState.CheckForState(States.FrostEnergy))
        {
            character.CharacterState.RemoveState(States.FrostEnergy);
        }
        else
        {
            Hero.CharacterState.CmdAddState(States.FrostEnergy, 999, 0f, Hero.gameObject, name);
        }
    }
}