using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbsotptionTalent : Talent
{
	[SerializeField] private Absorption _absorption;
	[SerializeField] private SkillManager _ability;
	public override void Enter()
	{
		if (_ability.Abilities.Contains(_absorption))
		{
			_absorption.enabled = true;
		}
		else
		{
			//_ability.AddAbility(_absorption);
		}
	}

	public override void Exit()
	{
		if (_ability.Abilities.Contains(_absorption))
		{
			//_ability.RemoveAbility(_absorption);
			_absorption.enabled = false;
		}
		else
		{
			_absorption.enabled = false;
			//ability.RemoveAbility(iceShard);
		}
	}
}
