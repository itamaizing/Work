using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_5 : Talent
{
    [SerializeField] private SeriesOfStrikes _seriesOfStrikes;

    public override void Enter()
    {
        _seriesOfStrikes.SeriesCompleteDoubleCombo(true);
        _seriesOfStrikes.SeriesCompliteCompoTalentActive(true);
    }

    public override void Exit()
    {
        _seriesOfStrikes.SeriesCompleteDoubleCombo(false);
        _seriesOfStrikes.SeriesCompliteCompoTalentActive(false);
    }
}
