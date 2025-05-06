using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SilenceTalentAttackMinion : Talent
{
    [SerializeField] private Silence silence;

	public override void Enter()
	{
		silence.SetCanAttackMinions(true);
	}

	public override void Exit()
	{
		silence.SetCanAttackMinions(false);
	}
}
