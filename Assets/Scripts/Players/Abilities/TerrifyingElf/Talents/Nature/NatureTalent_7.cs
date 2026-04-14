using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_7 : Talent
{
    [SerializeField] private ShotIntoSky shotIntoSky;
    [SerializeField] private ShotsIntoSky shotsIntoSky;

    public override void Enter()
    {
        shotIntoSky.SetSilenceTalentActive(true);
        shotsIntoSky.SetSilenceTalentActive(true);
    }

    public override void Exit()
    {
        shotIntoSky.SetSilenceTalentActive(false);
        shotsIntoSky.SetSilenceTalentActive(false);
    }
}
