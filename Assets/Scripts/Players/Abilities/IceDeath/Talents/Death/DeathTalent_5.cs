using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathTalent_5 : Talent
{
    [SerializeField] private NinjaResources _ninjaResources;

    public override void Enter()
    {
        _ninjaResources.EnergyToRestore(true, Data.DescriptionsForInfoPanel[0]);
    }

    public override void Exit()
    {
        _ninjaResources.EnergyToRestore(false, Data.DescriptionsForInfoPanel[0]);
    }
}
