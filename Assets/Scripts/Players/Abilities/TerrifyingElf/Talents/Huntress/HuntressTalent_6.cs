using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_6 : Talent
{
    [SerializeField] private ShotIntoSky shotIntoSky;
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;

    public override void Enter()
    {
        reconnaissanceFire.SkillEnableBoostLogicActiveTalent(true);
        shotIntoSky.ShotRadiusUpgradeActive(true);
    }

    public override void Exit()
    {
        reconnaissanceFire.SkillEnableBoostLogicActiveTalent(true);
        shotIntoSky.ShotRadiusUpgradeActive(false);
    }
}
