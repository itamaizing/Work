using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_11 : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private ShotIntoSky _shotIntoSky;
    [SerializeField] private GroundTrap _groundTrap;

    public override void Enter()
    {
        _shotIntoSky.SkillEnableBoostLogicActiveTalent(true);
        _groundTrap.SkillEnableBoostLogicActiveTalent(true);
        reconnaissanceFire.SkillEnableBoostLogicActiveTalent(true);
    }

    public override void Exit()
    {
        _shotIntoSky.SkillEnableBoostLogicActiveTalent(false);
        _groundTrap.SkillEnableBoostLogicActiveTalent(false);
        reconnaissanceFire.SkillEnableBoostLogicActiveTalent(false);
    }
}
