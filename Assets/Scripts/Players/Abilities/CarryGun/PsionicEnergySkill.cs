using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsionicEnergySkill : Skill, IPassiveSkill
{
    #region Skill
    protected override int AnimTriggerPrepare => throw new NotImplementedException();
    protected override int AnimTriggerCast => throw new NotImplementedException();
    public override void LoadTargetData(TargetInfo targetInfo) => throw new NotImplementedException();

    protected override IEnumerator CastJob()
    {
        yield return null;
    }

    protected override void ClearData() { }
    protected override IEnumerator TargetingJob(Action<TargetInfo> targetDataSavedCallback) => throw new NotImplementedException();
    #endregion

    [SerializeField] private BasePsionicEnergy basePsionicEnergy;
    [SerializeField] private float modifier = 1f;

    private const float PsiExplosionPercent = 0.3f;

    #region Talent
    private bool _isPsiEnergyActive = false;
    private bool _isDischargingPsiTalent = false;
    private bool _isExtendedDuration = false;
    
    public bool IsPsiEnergyActive { get => _isPsiEnergyActive; set => _isPsiEnergyActive = value;}
    public bool IsExtendedDuration { get => _isExtendedDuration; set => _isExtendedDuration = value; }

    public void PsiEnergyActive(bool value)
    {
        _isPsiEnergyActive = value;
    }

    public void ExtendedDuration(bool value) => _isExtendedDuration = value;
    #endregion
}
