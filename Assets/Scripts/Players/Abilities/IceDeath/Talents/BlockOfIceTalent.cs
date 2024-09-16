using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockOfIceTalent : Talent
{
	[SerializeField] private BlockOfIce _blockOfIce;
	[SerializeField] private SkillManager _ability;
	public override void Enter()
	{
		_ability.AddSkill(_blockOfIce);
	}

	public override void Exit()
	{
		_ability.RemoveSkill(_blockOfIce);
	}
}

