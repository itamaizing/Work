using Mirror;
using System.Collections;
using UnityEngine;

public class Conversion : Skill
{
    [SerializeField] private BasePsionicEnergy _psionicEnergy;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;

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
        if (_psionicEnergy != null && _attackingPsionicEnergy != null)
        {
            if (_psionicEnergy.CurrentValue > 0)
            {
                CmdConvertEnergy();
            }
        }
        yield return null;
    }

    [Command]
    private void CmdConvertEnergy()
    {
        _psionicEnergy.ConvertToAttackingEnergy(_attackingPsionicEnergy.MaxAttackingPsiEnergy);
    }
}
