using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_2 : Talent
{
	[SerializeField] private PhysicalAttack _physicalAttack;

	public override void Enter()
    {
		_physicalAttack.SeriesPhysicalTalentActive(true);
	}

    public override void Exit()
    {
		_physicalAttack.SeriesPhysicalTalentActive(false);
	}
}
