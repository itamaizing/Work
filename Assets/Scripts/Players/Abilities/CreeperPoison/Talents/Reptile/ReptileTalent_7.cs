using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReptileTalent_7 : Talent
{
    [SerializeField] private CreeperStrike _creeperStrike;

    public override void Enter()
    {
        _creeperStrike.SetReptileTalentActive(true);
    }

    public override void Exit()
    {
        _creeperStrike.SetReptileTalentActive(false);
    }
}

