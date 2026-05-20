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

    private bool _isBonusTalent;

    public bool IsBonusTalent => _isBonusTalent;

    public void ActivateSwiftBonus(bool value)
    {
        if(_isBonusTalent == value) return;
        _isBonusTalent = value;
        if(isClient)
            CmdActivateSwiftBonus(value);
    }

    [Command]
    private void CmdActivateSwiftBonus(bool value)
    {
        if(_isBonusTalent == value) return;
        _isBonusTalent = value;
    }

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
        if(!_isBonusTalent)
            _hero.CharacterState.AddState(States.SwiftAttacks, _buffDuration, 0, _hero.gameObject, nameof(SwiftAttacks_Scorpion));
        else
            _hero.CharacterState.AddState(States.SwiftAttacks, _buffDuration, 0, _hero.gameObject, nameof(SwiftAttacks_Scorpion)+"SwiftBonus");
    }

    protected override void ClearData() { }
}
