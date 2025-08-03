using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_1 : Talent
{
    [SerializeField] private GrowTree growTree;
	[SerializeField] private Silence silence;
    [SerializeField] private SkillManager _ability;

	public override void Enter()
	{
		_ability.ActivateSkill(growTree);
		_ability.ActivateSkill(silence);
	}

	public override void Exit()
	{
		_ability.DeactivateSkill(growTree);
		_ability.DeactivateSkill(silence);
	}
}
