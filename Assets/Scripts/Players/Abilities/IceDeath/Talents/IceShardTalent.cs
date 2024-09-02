using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceShardTalent : Talent
{
	[SerializeField] private IceShard _iceShard;
	[SerializeField] private SkillManager _ability;
	public override void Enter()
	{
		if(_ability.Abilities.Contains(_iceShard)) 
		{
			_iceShard.enabled = true;
		}
		else
		{
			//_ability.AddAbility(_iceShard);
			_iceShard.enabled = true;
		}
	}

	public override void Exit()
	{
		if (_ability.Abilities.Contains(_iceShard))
		{
			//_ability.RemoveAbility(_iceShard); 
			_iceShard.enabled = false;
			//iceShard.enabled = false;
		}
		else
		{
			_iceShard.enabled = false;
			//ability.RemoveAbility(iceShard);
		}
	}
}
