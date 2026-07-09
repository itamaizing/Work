using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_10 : Talent
{

    public override void Enter()
    {
        character.Abilities.GetSkill<ShotIntoSky>().ShotRadiusUpgradeActive(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ShotIntoSky>().ShotRadiusUpgradeActive(false);
    }
}
