using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceShardTalent : Talent
{
	[SerializeField] private IceShard iceShard;
	[SerializeField] private PlayerAbilities ability;
	public override void Enter()
	{
		if(ability.Abilities.Contains(iceShard)) 
		{

		}
		else
		{
			ability.AddAbility(iceShard);
		}
	}

	public override void Exit()
	{

	}
}
