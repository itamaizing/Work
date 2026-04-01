using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_10 : Talent
{
    [SerializeField] private SneakySpit _sneakySpit;
    [SerializeField] private PoisonSlap _poisonSlap;

    public override void Enter()
    {
        _sneakySpit.ColdBloodStrike(true);
        _poisonSlap.ColdBloodStrike(true);
    }

    public override void Exit()
    {
        _sneakySpit.ColdBloodStrike(false);
        _poisonSlap.ColdBloodStrike(false);
    }
}