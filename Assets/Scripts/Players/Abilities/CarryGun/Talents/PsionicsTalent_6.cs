using System;
using UnityEngine;

public class PsionicsTalent_6 : Talent
{
    [SerializeField] private BasePsionicEnergy _basePsionic;

    public override void Enter()
    {
        _basePsionic.AccumulationPsionicChanged(true);
    }

    public override void Exit()
    {
        _basePsionic.AccumulationPsionicChanged(false);
    }
}
