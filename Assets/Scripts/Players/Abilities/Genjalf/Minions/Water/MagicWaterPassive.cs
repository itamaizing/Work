using System;
using System.Collections;
using UnityEngine;

public class MagicWaterPassive : Skill, IPassiveSkill
{
    #region Skill
    protected override int AnimTriggerCastDelay { get; }
    protected override int AnimTriggerCast { get; }
    public override void LoadTargetData(TargetInfo targetInfo) => throw new NotImplementedException();

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) => throw new NotImplementedException();

    protected override IEnumerator CastJob() { yield return null; }

    protected override void ClearData() => throw new NotImplementedException();
    #endregion

    private void Start()
    {
        _hero.CharacterState.CmdAddState(States.MagicWater, 0, 0, _hero.gameObject, name);
    }
}
