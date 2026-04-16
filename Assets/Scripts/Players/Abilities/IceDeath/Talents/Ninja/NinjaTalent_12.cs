using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_12 : Talent
{
    [SerializeField] private SeriesOfStrikes _seriesOfStrikes;

    public override void Enter()
    {
        _seriesOfStrikes.SeriesCompleteDoubleCombo(true);
    }

    public override void Exit()
    {
        _seriesOfStrikes.SeriesCompleteDoubleCombo(false);
    }
}
