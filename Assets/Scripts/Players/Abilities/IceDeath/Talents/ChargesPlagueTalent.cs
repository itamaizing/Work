using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargesPlagueTalent : Talent
{
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private IceShard _iceShard;
	public override void Enter()
	{
		_deathSpiral.TalentChargesPlague(true);
		_iceShard.TalentChargesPlague(true);
	}

	public override void Exit()
	{
		_deathSpiral.TalentChargesPlague(false);
		_iceShard.TalentChargesPlague(false);
	}
}
