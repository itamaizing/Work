using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_11 : Talent
{
    [SerializeField] private SeriesOfStrikes _series;

    public override void Enter()
    {
        _series.SeriesAddNewCombo(true);
    }

    public override void Exit()
    {
        _series.SeriesAddNewCombo(false);
    }
}
