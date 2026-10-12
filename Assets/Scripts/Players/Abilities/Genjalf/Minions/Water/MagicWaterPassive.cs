using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class MagicWaterPassive : Skill, IPassiveSkill
{
    [SerializeField] private MagicWaterAura magicWaterAura;
    
    #region Skill
    protected override int AnimTriggerPrepare { get; }
    protected override int AnimTriggerCast { get; }
    public override void LoadTargetData(TargetInfo targetInfo) => throw new NotImplementedException();

    protected override IEnumerator TargetingJob(Action<TargetInfo> targetDataSavedCallback) => throw new NotImplementedException();

    protected override IEnumerator CastJob() { yield return null; }

    protected override void ClearData() => throw new NotImplementedException();
    #endregion

    public void EnableMagicWaterAura(bool value)
    {
        magicWaterAura.ActivateAura(value, isAffectOnOwner: true);
    }
}
