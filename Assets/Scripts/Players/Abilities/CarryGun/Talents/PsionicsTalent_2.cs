using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsionicsTalent_2 : Talent
{
    [SerializeField] private CheliceraStrike cheliceraStrike;

    public override void Enter()
    {
        cheliceraStrike.PsionicsTalentTwo(true);
    }

    public override void Exit()
    {
        cheliceraStrike.PsionicsTalentTwo(false);
    }
}
