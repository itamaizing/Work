using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_2 : Talent
{
    [SerializeField] private SeriesOfStrikes seriesOfStrikes;

    public override void Enter()
    {
        seriesOfStrikes.SeriesCompliteCompoTalentActive(true);
    }

    public override void Exit()
    {
        seriesOfStrikes.SeriesCompliteCompoTalentActive(false);
    }
}
