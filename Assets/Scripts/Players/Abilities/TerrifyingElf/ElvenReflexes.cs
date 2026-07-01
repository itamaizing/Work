using System;
using System.Collections;
using UnityEngine;

public class ElvenReflexes : Skill
{
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast =>
        _hero != null &&
        _hero.CharacterState.CheckForState(States.ElvenSkill) &&
        _hero.CharacterState.CheckStateStacks(States.ElvenSkill) > 0;

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        Disactive = true;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        callbackDataSaved(new TargetInfo());
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        if (_hero.CharacterState.CheckForState(States.ElvenReflexes))
        {
            if (isServer)
                _hero.CharacterState.RemoveState(States.ElvenReflexes);
            else
                _hero.CharacterState.CmdRemoveState(States.ElvenReflexes);
        }
        else
        {
            if (isServer)
                _hero.CharacterState.AddStateLogic(States.ElvenReflexes, -1f, 0f, Schools.None, _hero.gameObject, name);
            else
                _hero.CharacterState.CmdAddState(States.ElvenReflexes, -1f, 0f, Schools.None, _hero.gameObject, name);
        }

        yield break;
    }

    protected override void ClearData() { }

    public override void LoadTargetData(TargetInfo targetInfo) { }
}