using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_5 : Talent
{
    [SerializeField] private ShotAstral shotAstral;
    [SerializeField] private ShotIntoSky shotIntoSky;
    [SerializeField] private ShotsIntoSky shotsIntoSky;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(shotAstral);
        shotIntoSky.ShotsIntoSkyAstralTalentActive(true);
        shotsIntoSky.ShotsIntoSkyAstralTalentActive(true);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(shotAstral);
        shotIntoSky.ShotsIntoSkyAstralTalentActive(false);
        shotsIntoSky.ShotsIntoSkyAstralTalentActive(false);
    }
}
