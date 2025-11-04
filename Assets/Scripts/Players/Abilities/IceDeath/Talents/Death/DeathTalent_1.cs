using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathTalent_1 : Talent
{
    [SerializeField] private SeriesOfStrikes runeComponent;
    [SerializeField] private NinjaResources ninjaResources;

    public override void Enter()
    {
        runeComponent.IceRuneTalentActive(true);
        ninjaResources.EnergyToRestore(true, Data.DescriptionsForInfoPanel[0]);
    }

    public override void Exit()
    {
        runeComponent.IceRuneTalentActive(false);
        ninjaResources.EnergyToRestore(false, Data.DescriptionsForInfoPanel[0]);
    }
}
