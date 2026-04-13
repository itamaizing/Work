using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_8 : Talent
{
    [SerializeField] private ShotIntoSky shotIntoSky;

    public override void Enter()
    {
        shotIntoSky.ShotRadiusUpgradeActive(true);
    }

    public override void Exit()
    {
        shotIntoSky.ShotRadiusUpgradeActive(false);
    }
}
