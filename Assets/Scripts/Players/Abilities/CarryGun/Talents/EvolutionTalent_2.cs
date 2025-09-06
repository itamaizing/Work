using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionTalent_2 : Talent
{
    [SerializeField] private CheliceraStrike cheliceraStrike;
    [SerializeField] private ClawStrike clawStrike;

    public override void Enter()
    {
        clawStrike.BleedingClawStrike(true);
        cheliceraStrike.EvolutionTalentTwo(true);
    }

    public override void Exit()
    {
        clawStrike.BleedingClawStrike(false);
        cheliceraStrike.EvolutionTalentTwo(false);
    }
}
