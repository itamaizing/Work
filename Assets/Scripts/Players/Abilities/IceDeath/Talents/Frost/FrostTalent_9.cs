using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_9 : Talent
{
	[SerializeField] private NinjaResources _ninjaResources;

	public override void Enter()
	{
		_ninjaResources.RepeatedFrost(true);
	}

	public override void Exit()
	{
		_ninjaResources.RepeatedFrost(false);
	}
}
