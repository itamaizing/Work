using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_7 : Talent
{
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;

    public override void Enter()
    {
        _creeperPoisonAura.EvadePoison(true);
    }

    public override void Exit()
    {
        _creeperPoisonAura.EvadePoison(false);
    }
}