using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class MergeWithDarknessSkill : Skill
{
    [SerializeField] private float _duration = 4f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo) { }

    protected override void ClearData() { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(_hero);
        targetDataSavedCallback(targetInfo);
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        CmdApplyState();
        yield return null;
    }

    [Command]
    private void CmdApplyState()
    {
        Hero.CharacterState.AddState(
            States.MergeDark,
            _duration,
            0,
            Hero.gameObject,
            name
        );
    }
}
