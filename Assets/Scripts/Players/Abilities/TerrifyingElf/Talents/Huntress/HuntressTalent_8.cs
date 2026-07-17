using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_8 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<GrowTree>().GrowTreeArrowIntoSkyRadius(true);
        character.Abilities.GetSkill<ReconnaissanceFire>().FireArrowIntoSkyRadius(true);
        character.Abilities.GetSkill<GroundTrap>().TrapArrowIntoSkyRadius(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<GrowTree>().GrowTreeArrowIntoSkyRadius(false);
        character.Abilities.GetSkill<ReconnaissanceFire>().FireArrowIntoSkyRadius(false);
        character.Abilities.GetSkill<GroundTrap>().TrapArrowIntoSkyRadius(false);
    }
}
