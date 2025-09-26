using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_4 : Talent
{
    [SerializeField] private IcePuddle icePuddle;

    public override void Enter()
    {
        icePuddle.IceDeathInIcePudleTalentActive(true, Data.DescriptionsForInfoPanel[1]);
    }

    public override void Exit()
    {
        icePuddle.IceDeathInIcePudleTalentActive(false, Data.DescriptionsForInfoPanel[1]);
    }
}
