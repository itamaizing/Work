using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheliceraStrikeTalent : Talent
{
    [SerializeField] private CheliceraStrike cheliceraStrike;

    public override void Enter()
    {
        cheliceraStrike.CheliceraStrikeTalent(true);
    }

    public override void Exit()
    {
        cheliceraStrike.CheliceraStrikeTalent(false);
    }
}
