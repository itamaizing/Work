using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsionicsTalent_3 : Talent
{
    [SerializeField] private Tentacles tentacles;

    public override void Enter()
    {
        tentacles.CurrentTentacle.PsionicsTalentThree(true);
    }

    public override void Exit()
    {
        tentacles.CurrentTentacle.PsionicsTalentThree(false);
    }
}
