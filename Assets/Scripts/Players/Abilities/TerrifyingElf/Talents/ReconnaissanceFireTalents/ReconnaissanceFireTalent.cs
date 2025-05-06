using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReconnaissanceFireTalent : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private SkillManager _ability;

	public override void Enter()
	{
		_ability.ActivateSkill(reconnaissanceFire);
	}

	public override void Exit()
	{
		_ability.DeactivateSkill(reconnaissanceFire);
	}
}
