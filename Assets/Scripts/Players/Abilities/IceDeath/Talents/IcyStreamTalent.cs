using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcyStreamTalent : Talent
{
	[SerializeField] private IcyStream _icyStream;
	[SerializeField] private SkillManager _ability;
	public override void Enter()
	{
		if (_ability.Abilities.Contains(_icyStream))
		{
			_icyStream.enabled = true;
		}
		else
		{
			//_ability.AddAbility(_icyStream);
		}
	}

	public override void Exit()
	{
		if (_ability.Abilities.Contains(_icyStream))
		{
			//_ability.RemoveAbility(_icyStream);
			_icyStream.enabled = false;
		}
		else
		{
			_icyStream.enabled = false;
			//ability.RemoveAbility(iceShard);
		}
	}
}
