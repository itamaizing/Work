using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostingFrozenTalant : Talent
{
	[SerializeField] private IcePuddle _icePuddle;
	public override void Enter()
	{
		_icePuddle.SetTalentFrostingFrozen(true);
	}

	public override void Exit()
	{
		_icePuddle.SetTalentFrostingFrozen(true);
	}

}
