using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_4 : Talent
{
    [SerializeField] private ShotIntoSky shotIntoSky;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private SkillManager ability;

    public override void Enter()
    {
        ability.ActivateSkill(shotIntoSky);
        terrifyingElfAura.ElvenSkillTalent(true);
    }

    public override void Exit()
    {
        ability.DeactivateSkill(shotIntoSky);
        terrifyingElfAura.ElvenSkillTalent(false);
    }
}
