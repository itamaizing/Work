using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcePuddleTalent : Talent
{
	[SerializeField] private IcePuddle _icePuddleAbility;
	public override void Enter()
	{
		_icePuddleAbility.SetTalentActive(true);
	}

	public override void Exit()
	{
		_icePuddleAbility.SetTalentActive(false);
	}
}
