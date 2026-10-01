using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_6 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ReconnaissanceFire>().FireWorshipperTalentActive(true);
        //terrifyingElfAura.ElvenSkillTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ReconnaissanceFire>().FireWorshipperTalentActive(false);
        //terrifyingElfAura.ElvenSkillTalent(false);
    }
}
