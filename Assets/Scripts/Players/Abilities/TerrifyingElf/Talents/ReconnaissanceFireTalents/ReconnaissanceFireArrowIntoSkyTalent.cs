using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReconnaissanceFireArrowIntoSkyTalent : Talent
{
    [SerializeField] private ShotIntoSky shotIntoSky;
    [SerializeField] private ShotsIntoSky shotsIntoSky;

    public override void Enter()
    {
        shotIntoSky.SetTripleShotTalentActive(true);
        shotsIntoSky.SetTripleShotTalentActive(true);
    }

    public override void Exit()
    {
        shotIntoSky.SetTripleShotTalentActive(false);
        shotsIntoSky.SetTripleShotTalentActive(false);
    }
}
