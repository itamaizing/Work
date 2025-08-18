using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntressTalent_1 : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private SkillManager ability;
	[SerializeField] private Ghost ghost;
	[SerializeField] private VisionComponent visionComponent;
	[SerializeField] private Skill skill;

	public override void Enter()
	{
		ability.ActivateSkill(reconnaissanceFire);
		ghost.MovingToGhostWithZeroMana(true);
		visionComponent.VisionRange += 3;
		skill.Radius += 1.5f;
	}

	public override void Exit()
	{
		ability.DeactivateSkill(reconnaissanceFire);
		ghost.MovingToGhostWithZeroMana(false);
		visionComponent.VisionRange -= 3;
		skill.Radius -= 1.5f;
	}
}
