using System;
using System.Collections;
using UnityEngine;

public class ProtectiveScales : Skill
{
    [SerializeField] private GameObject baseBody;
    [SerializeField] private GameObject protectiveScalesBody;

    private const float Duration = 2;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    public GameObject BaseBody { get => baseBody; set => baseBody = value; }
    public GameObject ProtectiveScalesBody { get => protectiveScalesBody; set => protectiveScalesBody = value; }

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
        if (Hero == null || Hero.CharacterState == null)
            yield break;

        Hero.CharacterState.CmdAddState(States.ProtectiveScales, Duration, 0f, Hero.gameObject, name);
    }
}
