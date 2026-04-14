using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_11 : Talent
{
    [SerializeField] private ShotIntoSky shotIntoSky;
    [SerializeField] private ShotsIntoSky shotsIntoSky;

    public override void Enter()
    {
        shotIntoSky.ShotsIntoSkyAstralTalentActive(true);
        shotsIntoSky.ShotsIntoSkyAstralTalentActive(true);
    }

    public override void Exit()
    {
        shotIntoSky.ShotsIntoSkyAstralTalentActive(false);
        shotsIntoSky.ShotsIntoSkyAstralTalentActive(false);
    }
}
