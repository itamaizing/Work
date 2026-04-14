using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisciplineTalent_4 : Talent
{
    public override void Enter()
    {

        character.Abilities.GetSkill<PriestShield>()?.EnableBooster(PriestShield.PriestShieldBoosterType.DisciplineShieldBoost, true);
        character.Abilities.GetSkill<SoulAid>()?.EnableCooldownReduce(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<PriestShield>()?.EnableBooster(PriestShield.PriestShieldBoosterType.DisciplineShieldBoost, false);
        character.Abilities.GetSkill<SoulAid>()?.EnableCooldownReduce(false);
    }
}
