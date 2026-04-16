using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_4 : Talent
{
    [SerializeField] private IcePuddle icePuddle;

    [SerializeField] private SeriesOfStrikes _seriesOfStrikes;

    public override void Enter()
    {
        _seriesOfStrikes.SeriesCompliteCompoTalentActive(true);
        //icePuddle.IceDeathInIcePudleTalentActive(true, "");
    }

    public override void Exit()
    {
        _seriesOfStrikes.SeriesCompliteCompoTalentActive(false);
        //icePuddle.IceDeathInIcePudleTalentActive(false, "");
    }
}
