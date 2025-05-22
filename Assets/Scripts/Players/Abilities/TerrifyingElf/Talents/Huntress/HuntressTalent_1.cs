using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_1 : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private SkillManager ability;
	[SerializeField] private Ghost ghost;

	public override void Enter()
	{
		ability.ActivateSkill(reconnaissanceFire);
		ghost.MovingToGhostWithZeroMana(true);
	}

	public override void Exit()
	{
		ability.DeactivateSkill(reconnaissanceFire);
		ghost.MovingToGhostWithZeroMana(false);
	}
}
