using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReptileTalent_8 : Talent
{
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;

    public override void Enter()
    {
        _creeperPoisonAura.PleasurePoisoning(true);
    }

    public override void Exit()
    {
        _creeperPoisonAura.PleasurePoisoning(false);
    }
}

