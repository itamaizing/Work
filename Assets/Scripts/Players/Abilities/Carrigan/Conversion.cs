using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class Conversion : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _psionicEnergy;
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => _psionicEnergy.CurrentValue > 0;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        
    }

    protected override void ClearData()
    {
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        if (_psionicEnergy != null)
        {
            if (_psionicEnergy.CurrentValue > 0)
            {
                CmdEnabled();
            }
        }
        yield return null;
    }

    [Command]
    private void CmdEnabled()
    {
        _psionicEnergy.ActivateAttackingEnergy();
    }

}
