using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionTalent_3 : Talent
{
    [SerializeField] private CheliceraStrike cheliceraStrike;
    [SerializeField] private ClawStrike clawStrike;

    public override void Enter()
    {
        cheliceraStrike.CheliceraStrikeSpeed(true);
        clawStrike.ClawStrikeSpeed(true);
    }

    public override void Exit()
    {
        cheliceraStrike.CheliceraStrikeSpeed(false);
        clawStrike.ClawStrikeSpeed(false);
    }
}
