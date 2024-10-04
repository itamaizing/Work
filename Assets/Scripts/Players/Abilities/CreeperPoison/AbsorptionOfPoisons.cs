using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbsorptionOfPoisons : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CreeperStrike _creeperStrike;

    protected override bool IsCanCast => throw new System.NotImplementedException();

    protected override void ClearData()
    {
        throw new System.NotImplementedException();

    }

    protected override IEnumerator PrepareJob()
    {
        throw new System.NotImplementedException();
    }

    protected override IEnumerator CastJob()
    {
        throw new System.NotImplementedException();
    }
}
