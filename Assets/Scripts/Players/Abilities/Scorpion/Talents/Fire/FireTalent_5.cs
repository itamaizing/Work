using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTalent_5 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<Teleportation_Scorpion>().EnableScorchedSoulDiscount(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<Teleportation_Scorpion>().EnableScorchedSoulDiscount(false);
    }
}
