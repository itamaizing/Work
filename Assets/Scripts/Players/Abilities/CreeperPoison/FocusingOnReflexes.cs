using System;
using System.Collections;
using UnityEngine;

public class FocusingOnReflexes : Skill
{
    [SerializeField] private float duration = 1f;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.Targets.Contains(Hero)) return;
        targetInfo.Targets.Add(Hero);
    }

    protected override void ClearData() { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(Hero);
        callbackDataSaved(targetInfo);

        yield break;
    }

    protected override IEnumerator CastJob()
    {
        Hero.CharacterState.CmdAddState(States.FocusingOnReflexesState, duration, 0f, Hero.gameObject, name);
        yield break;
    }
}
