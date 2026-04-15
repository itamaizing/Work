using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_4 : Talent
{
    [SerializeField] private IceShadow iceShadow;
    [SerializeField] private IcePuddle icePuddle;

    [SerializeField] private SeriesOfStrikes _seriesOfStrikes;

    public override void Enter()
    {
        iceShadow.IceDeathInShadowTalentActive(true, Data.DescriptionsForInfoPanel[0]);
        _seriesOfStrikes.SeriesCompliteCompoTalentActive(true);
        //icePuddle.IceDeathInIcePudleTalentActive(true, "");
    }

    public override void Exit()
    {
        iceShadow.IceDeathInShadowTalentActive(false, Data.DescriptionsForInfoPanel[0]);
        _seriesOfStrikes.SeriesCompliteCompoTalentActive(false);
        //icePuddle.IceDeathInIcePudleTalentActive(false, "");
    }
}
