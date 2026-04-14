using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_12 : Talent
{
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;

    public override void Enter()
    {
        _creeperPoisonAura.FeelingPoisoning(true);
    }

    public override void Exit()
    {
        _creeperPoisonAura.FeelingPoisoning(false);
    }
}
