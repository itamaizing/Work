using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SwiftAttacks_Scorpion : Skill
{
    [SerializeField] private float _buffDuration = 3f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo) { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo info = new TargetInfo();
        callbackDataSaved(info);
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        CmdAddSwiftAttack();

        yield return null;
    }

    [Command]
    private void CmdAddSwiftAttack()
    {
        _hero.CharacterState.AddState(States.SwiftAttacks, _buffDuration, 0, _hero.gameObject, Name);
    }

    protected override void ClearData() { }
}
