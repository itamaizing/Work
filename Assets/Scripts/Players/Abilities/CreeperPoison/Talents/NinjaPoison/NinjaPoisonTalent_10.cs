using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_10 : Talent
{
    [SerializeField] private SneakySpit _sneakySpit;

    public override void Enter()
    {
        _sneakySpit.ColdBloodStrike(true);
    }

    public override void Exit()
    {
        _sneakySpit.ColdBloodStrike(false);
    }
}