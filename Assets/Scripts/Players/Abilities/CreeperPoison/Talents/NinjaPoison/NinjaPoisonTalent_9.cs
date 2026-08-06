using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_9 : Talent
{
    [SerializeField] private CreeperStrike _creeperStrike;

    public override void Enter()
    {
        character.Abilities.GetSkill<ColdBlood>().SetTalentActive(true);
        //_creeperStrike.ColdBloodStrike(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ColdBlood>().SetTalentActive(false);
        //_creeperStrike.ColdBloodStrike(false);
    }
}