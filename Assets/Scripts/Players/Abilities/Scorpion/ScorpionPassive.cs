using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScorpionPassive : Skill, IPassiveSkill
{
    #region Skill
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    public override void LoadTargetData(TargetInfo targetInfo) { }

    protected override IEnumerator CastJob() { yield return null; }

    protected override void ClearData() { }
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) { yield return null; }
    #endregion
    [NonSerialized] public bool IsEnergyFreeAfterTeleport = false;

    #region Talent
    private bool _isAddStateUpdateChance = false;
    private bool _isImpulseMatter = false;

    public bool IsAddStateUpdateChance { get => _isAddStateUpdateChance; set => _isAddStateUpdateChance = value; }
    public bool IsImpulseMatter { get => _isImpulseMatter; }

    public void AddStateUpdateChance(bool value) => _isAddStateUpdateChance = value;
    public void ImpulseMatter(bool value) => _isImpulseMatter = value;
    #endregion

    public void ActivateEnergyFreeAfterTeleport()
    {
        if (_isImpulseMatter) _hero.Abilities.SetNextSkillFree();
    }
}
