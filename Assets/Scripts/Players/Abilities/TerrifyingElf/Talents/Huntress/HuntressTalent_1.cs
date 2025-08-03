using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_1 : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private SkillManager ability;

	public override void Enter()
	{
		ability.ActivateSkill(reconnaissanceFire);
	}

	public override void Exit()
	{
		ability.DeactivateSkill(reconnaissanceFire);
	}
}
