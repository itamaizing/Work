using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_5 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ComboSeriesSystem>().EnableAdditionalRuneOnSeries(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ComboSeriesSystem>().EnableAdditionalRuneOnSeries(false);
    }
}
