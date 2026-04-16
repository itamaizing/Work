using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_8 : Talent
{
    [SerializeField] private SeriesOfStrikes _seriesOfStrikes;

    public override void Enter()
    {
        _seriesOfStrikes.SeriesCompliteCompoTalentActive(true);
    }

    public override void Exit()
    {
        _seriesOfStrikes.SeriesCompliteCompoTalentActive(false);
    }
}
