using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockOfIceTalent : Talent
{
	[SerializeField] private BlockOfIce _blockOfIce;
	[SerializeField] private SkillManager _ability;
	public override void Enter()
	{
		if (_ability.Abilities.Contains(_blockOfIce))
		{
			_blockOfIce.enabled = true;
		}
		else
		{
			//_ability.AddAbility(_blockOfIce);
		}
	}

	public override void Exit()
	{
		if (_ability.Abilities.Contains(_blockOfIce))
		{
			//_ability.RemoveAbility(_blockOfIce);
			_blockOfIce.enabled = false;
		}
		else
		{
			_blockOfIce.enabled = false;
			//ability.RemoveAbility(iceShard);
		}
	}
}
