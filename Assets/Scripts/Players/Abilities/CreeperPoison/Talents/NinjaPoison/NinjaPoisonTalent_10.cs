using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_10 : Talent
{
    [SerializeField] private SneakySpit _sneakySpit;
    [SerializeField] private PoisonSlap _poisonSlap;
    [SerializeField] private PoisonBall _poisonBall;

    public override void Enter()
    {
        _sneakySpit.ColdBloodStrike(true);
        _poisonSlap.ColdBloodStrike(true);
        _poisonBall.ColdBloodStrike(true);
    }

    public override void Exit()
    {
        _sneakySpit.ColdBloodStrike(false);
        _poisonSlap.ColdBloodStrike(false);
        _poisonBall.ColdBloodStrike(false);
    }
}