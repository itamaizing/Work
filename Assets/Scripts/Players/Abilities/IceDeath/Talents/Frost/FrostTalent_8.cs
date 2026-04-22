using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_8 : Talent
{
	[SerializeField] private FrostEnergy _frostEnergy;

	public override void Enter()
	{
		_frostEnergy._UseRuneBonusEffect(true);
	}

	public override void Exit()
	{
		_frostEnergy._UseRuneBonusEffect(false);
	}
}
