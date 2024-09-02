using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicDefenseTalent : Talent
{
	[SerializeField] private MagicDefense _magicDefense;
	[SerializeField] private SkillManager _ability;
	public override void Enter()
	{
		if (_ability.Abilities.Contains(_magicDefense))
		{
			_magicDefense.enabled = true;
		}
		else
		{
			//_ability.AddAbility(_magicDefense);
			_magicDefense.enabled = true;
		}
	}

	public override void Exit()
	{
		if (_ability.Abilities.Contains(_magicDefense))
		{
			//_ability.RemoveAbility(_magicDefense);
			_magicDefense.enabled = false;
		}
		else
		{
			_magicDefense.enabled = false;
			//ability.RemoveAbility(iceShard);
		}
	}
}
