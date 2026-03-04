using System;
using UnityEngine;

public class PsionicsTalent_13 : Talent
{
    [SerializeField] private BasePsionicEnergy _basePsionic;

    public override void Enter()
    {
        _basePsionic.TakesAnyDamage(true);
    }

    public override void Exit()
    {
        _basePsionic.TakesAnyDamage(false); ;
    }
}
