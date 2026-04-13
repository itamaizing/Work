using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_8 : Talent
{
    [SerializeField] private Silence silence;
	[SerializeField] private PullingHealth pulling;

	public override void Enter()
	{
		pulling.PullingHealthThroughGhosts(true);
		silence.SetCanAttackMinions(true);
		silence.SetReducedCooldown(true);
	}

	public override void Exit()
	{
		pulling.PullingHealthThroughGhosts(false);
		silence.SetCanAttackMinions(false);
		silence.SetReducedCooldown(true);
	}
}
