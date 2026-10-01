using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_5 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private SleepSpell sleep;

    public override void Enter()
    {
        character.Abilities.GetSkill<GrowTree>().TreeManaRegenTalentActive(true);
        //terrifyingElfAura.TreeRadiusCalmessTalentActive(true);
        //sleep.SleepInnerDarknessTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<GrowTree>().TreeManaRegenTalentActive(false);
        //terrifyingElfAura.TreeRadiusCalmessTalentActive(false);
        //sleep.SleepInnerDarknessTalent(true);
    }
}
