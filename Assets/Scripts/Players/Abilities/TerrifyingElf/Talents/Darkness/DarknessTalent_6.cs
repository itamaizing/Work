using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_6 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<RetributiveReckoning>().MagicAbilityInstantly(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<RetributiveReckoning>().MagicAbilityInstantly(false);
    }
}
