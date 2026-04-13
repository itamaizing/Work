using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_11 : Talent
{
    [SerializeField] private ShotIntoSky shotIntoSky;
    [SerializeField] private ShotsIntoSky shotsIntoSky;

    public override void Enter()
    {
        shotIntoSky.ShotsIntoSkyMagicDebuffTalentActive(true);
        shotsIntoSky.ShotsIntoSkyMagicDebuffTalentActive(true);
    }

    public override void Exit()
    {
        shotIntoSky.ShotsIntoSkyMagicDebuffTalentActive(false);
        shotsIntoSky.ShotsIntoSkyMagicDebuffTalentActive(false);
    }
}
