using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Conversion : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _psionicEnergy;
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => _psionicEnergy.CurrentValue > 0;

    protected override void ClearData()
    {
    }

    protected override IEnumerator PrepareJob()
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
