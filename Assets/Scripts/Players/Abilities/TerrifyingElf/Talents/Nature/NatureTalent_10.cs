using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_10 : Talent
{
    [SerializeField] private ShotAstral shotAstral;
    [SerializeField] private ShotIntoSky shotIntoSky;
    [SerializeField] private ShotsIntoSky shotsIntoSky;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private SkillManager ability;

    public override void Enter()
    {
        ability.ActivateSkill(shotAstral);
        shotIntoSky.ShotsIntoSkyAstralTalentActive(true);
        shotsIntoSky.ShotsIntoSkyAstralTalentActive(true);
        terrifyingElfAura.TreeRadiusCalmessTalentActive(true);
    }

    public override void Exit()
    {
        ability.DeactivateSkill(shotAstral);
        shotIntoSky.ShotsIntoSkyAstralTalentActive(false);
        shotsIntoSky.ShotsIntoSkyAstralTalentActive(false);
        terrifyingElfAura.TreeRadiusCalmessTalentActive(false);
    }
}
