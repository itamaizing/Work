using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class FocusingOnReflexes : Skill
{
    [SerializeField] private float duration = 1f;

    private Coroutine _buffTimerCoroutine;
    private bool _isBuffActive;


    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;


    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.GetTargets().Contains(Hero)) return;
        targetInfo.AddTarget(Hero);
    }

    protected override void ClearData() { }

    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);
        callbackDataSaved(targetInfo);
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        ApplyBuff();
        yield break;
    }

    [Command]
    private void ApplyBuff()
    {
        Hero.CharacterState.AddState(States.FocusingOnReflexesState, duration, 0f, Hero.gameObject, name);
    }
}