using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_4 : Talent
{
    [SerializeField] private CreeperStrike creeperStrike;

    public override void Enter()
    {
        creeperStrike.CheckForStatePoisonBone(true);
    }

    public override void Exit()
    {
        creeperStrike.CheckForStatePoisonBone(false);
    }
}