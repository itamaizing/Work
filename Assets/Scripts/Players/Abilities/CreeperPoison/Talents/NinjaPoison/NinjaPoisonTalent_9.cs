using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_9 : Talent
{
    [SerializeField] private CreeperStrike _creeperStrike;

    public override void Enter()
    {
        _creeperStrike.ColdBloodStrike(true);
    }

    public override void Exit()
    {
        _creeperStrike.ColdBloodStrike(false);
    }
}