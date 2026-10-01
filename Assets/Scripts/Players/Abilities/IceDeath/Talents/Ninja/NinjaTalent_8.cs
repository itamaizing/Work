using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_8 : Talent
{
    public override void Enter()
    {
       character.Abilities.GetSkill<ComboSeriesSystem>().EnableSpeedIncreasedOnSeries(true);
    }

    public override void Exit()
    {
       character.Abilities.GetSkill<ComboSeriesSystem>().EnableSpeedIncreasedOnSeries(false);
    }
}
