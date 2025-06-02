using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsionicsTalent_3 : Talent
{
    [SerializeField] private Tentacles tentacles;

    public override void Enter()
    {
        tentacles.PsionicsTalentThree(true);
    }

    public override void Exit()
    {
        tentacles.PsionicsTalentThree(false);
    }
}
