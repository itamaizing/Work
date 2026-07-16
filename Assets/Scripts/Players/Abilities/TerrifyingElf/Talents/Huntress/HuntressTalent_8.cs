using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_8 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<GrowTree>().GrowTreeArrowIntoSkyRadius(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<GrowTree>().GrowTreeArrowIntoSkyRadius(false);
    }
}
