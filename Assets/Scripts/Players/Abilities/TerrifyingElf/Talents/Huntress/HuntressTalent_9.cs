using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_9 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<TerrifyingElfAura>().IncreaseManaRegen(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<TerrifyingElfAura>().IncreaseManaRegen(false);
    }
}
