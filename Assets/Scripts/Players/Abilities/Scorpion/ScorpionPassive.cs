using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScorpionPassive : Skill, IPassiveSkill
{
    #region Skill
    protected override int AnimTriggerCastDelay => throw new NotImplementedException();
    protected override int AnimTriggerCast => throw new NotImplementedException();
    public override void LoadTargetData(TargetInfo targetInfo) => throw new NotImplementedException();

    protected override IEnumerator CastJob()
    {
        yield return null;
    }

    protected override void ClearData() => throw new NotImplementedException();
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) => throw new NotImplementedException();
    #endregion

    #region Talent
    private bool _isAddStateUpdateChance = false;

    public bool IsAddStateUpdateChance { get; set; }

    public void AddStateUpdateChance(bool value) => _isAddStateUpdateChance = value;
    #endregion
}
