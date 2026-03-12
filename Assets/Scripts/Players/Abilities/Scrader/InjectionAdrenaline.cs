using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InjectionAdrenaline : Skill
{
    [SerializeField] private float _duration = 5f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => true;

    protected override IEnumerator CastJob()
    {
        if (Hero == null || Hero.CharacterState == null) yield break;

        Hero.CharacterState.CmdAddState(States.InjectionAdrenaline, _duration, 0f, Hero.gameObject, name);
    }

    private void OnDisable()
    {

    }

    protected override void ClearData()
    {
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.GetTargets().Contains(Hero)) return;

        targetInfo.AddTarget(Hero);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);

        callbackDataSaved(targetInfo);
        yield break;
    }
}
