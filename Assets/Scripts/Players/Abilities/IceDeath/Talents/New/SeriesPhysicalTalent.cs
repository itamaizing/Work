using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeriesPhysicalTalent : Talent
{
    [SerializeField] private PhysicalAttack physicalAttack;

    public override void Enter()
    {
        physicalAttack.SeriesPhysicalTalentActive(true);
    }

    public override void Exit()
    {
        physicalAttack.SeriesPhysicalTalentActive(false);
    }
}
