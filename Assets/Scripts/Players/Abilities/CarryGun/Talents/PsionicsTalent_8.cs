using System;
using UnityEngine;

public class PsionicsTalent_8 : Talent
{
    [SerializeField] private BasePsionicEnergy _basePsionic;

    public override void Enter()
    {
        _basePsionic.AccumulationPsionicRunning(true);
    }

    public override void Exit()
    {
        _basePsionic.AccumulationPsionicRunning(false);
    }
}
