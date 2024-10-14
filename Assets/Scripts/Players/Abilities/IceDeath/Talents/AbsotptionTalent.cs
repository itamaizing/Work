using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbsotptionTalent : Talent
{
	[SerializeField] private Absorption _absorption;
	[SerializeField] private SkillManager _ability;
	public override void Enter()
	{
		_ability.AddSkill(_absorption);
	}

	public override void Exit()
	{
		_ability.RemoveSkill(_absorption);
	}

	private void Update()
	{
		if(Input.GetKeyUp(KeyCode.R)) 
		{
			//Enter();
		}
	}
}
