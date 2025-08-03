using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticsTalent_1 : Talent
{
    [SerializeField] private CreeperStrike creeperStrike;

    public override void Enter()
    {
        creeperStrike.GeneticsTalentOne(true);
    }

    public override void Exit()
    {
        creeperStrike.GeneticsTalentOne(false);
    }
}
