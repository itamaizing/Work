using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_11 : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;

    public override void Enter()
    {
        reconnaissanceFire.SkillEnableBoostLogicActiveTalent(true);
    }

    public override void Exit()
    {
        reconnaissanceFire.SkillEnableBoostLogicActiveTalent(false);
    }
}
