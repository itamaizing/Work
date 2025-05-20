using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_9 : Talent
{
    [SerializeField] private Silence silence;

	public override void Enter()
	{
		silence.SetCanAttackMinions(true);
		silence.SetReducedCooldown(true);
	}

	public override void Exit()
	{
		silence.SetCanAttackMinions(false);
		silence.SetReducedCooldown(true);
	}
}
