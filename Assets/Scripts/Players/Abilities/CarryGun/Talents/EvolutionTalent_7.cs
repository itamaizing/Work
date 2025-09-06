using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionTalent_7 : Talent
{
    [SerializeField] private CheliceraStrike cheliceraStrike;

    public override void Enter()
    {
        cheliceraStrike.ChanceCritDamageIncrease(true);
    }

    public override void Exit()
    {
        cheliceraStrike.ChanceCritDamageIncrease(false);
    }
}
