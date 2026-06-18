using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_12 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<NinjaResources>().EnableDeepFrosting(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<NinjaResources>().EnableDeepFrosting(false);
    }
}
