using System;
using System.Collections;
using UnityEngine;

public class MetabolismReptile : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private float _duration = 3f;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerPrepare => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo) { }

    protected override IEnumerator TargetingJob(Action<TargetInfo> targetDataSavedCallback)
    {
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        _player.CharacterState.CmdAddState(States.MetabolismReptile, _duration, 0f, _player.gameObject, "MetabolismReptile");
        yield return null;
    }

    protected override void ClearData() { }
}