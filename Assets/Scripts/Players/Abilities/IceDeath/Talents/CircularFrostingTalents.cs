using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircularFrostingTalents : Talent
{
	[SerializeField] private CircularFrosting _circularFrosting;
	[SerializeField] private SkillManager _ability;
	public override void Enter()
	{
		_ability.AddSkill(_circularFrosting);
	}

	public override void Exit()
	{
		_ability.RemoveSkill(_circularFrosting);
	}
}
