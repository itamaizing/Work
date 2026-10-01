using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_11 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ComboSeriesSystem>().AddNewPatterns(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ComboSeriesSystem>().AddNewPatterns(false);
    }
}
