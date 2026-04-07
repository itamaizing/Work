using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_9 : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;

    public override void Enter()
    {
        reconnaissanceFire.FireWorshipperTalentActive(true);
    }

    public override void Exit()
    {
        reconnaissanceFire.FireWorshipperTalentActive(false);
    }
}
