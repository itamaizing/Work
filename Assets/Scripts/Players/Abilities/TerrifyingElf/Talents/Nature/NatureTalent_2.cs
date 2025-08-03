using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_2 : Talent
{
    [SerializeField] private ShotIntoSky shotIntoSky;
    [SerializeField] private ShotsIntoSky shotsIntoSky;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        shotIntoSky.SetSilenceTalentActive(true);
        shotsIntoSky.SetSilenceTalentActive(true);
        terrifyingElfAura.TreeRadiusCalmessTalentActive(true);
    }

    public override void Exit()
    {
        shotIntoSky.SetSilenceTalentActive(false);
        shotsIntoSky.SetSilenceTalentActive(false);
        terrifyingElfAura.TreeRadiusCalmessTalentActive(false);
    }
}
