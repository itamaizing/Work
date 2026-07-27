using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_2 : Talent
{
	[SerializeField] private ComboSeriesSystem _comboSeries;

	public override void Enter()
    {
	    _comboSeries.EnableSeries(true);
	}

    public override void Exit()
    {
	    _comboSeries.EnableSeries(false);
	}
}
