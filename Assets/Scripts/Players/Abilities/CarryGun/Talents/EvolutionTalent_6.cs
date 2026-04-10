using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionTalent_6 : Talent
{
    [SerializeField] private ClawStrike clawStrike;

    public override void Enter()
    {
        clawStrike.BleedingClawStrike(true);
    }

    public override void Exit()
    {
        clawStrike.BleedingClawStrike(false);
    }
}
