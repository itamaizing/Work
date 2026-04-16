using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_14 : Talent
{
    [SerializeField] private IceShadow iceShadow;

    public override void Enter()
    {
        iceShadow.IceDeathInShadowTalentActive(true, Data.DescriptionsForInfoPanel[0]);
    }

    public override void Exit()
    {
        iceShadow.IceDeathInShadowTalentActive(false, Data.DescriptionsForInfoPanel[0]);
    }
}
