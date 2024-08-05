using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSpiralTalents : Talent
{
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private PlayerAbilities _ability;
	public override void Enter()
	{
		if (_ability.Abilities.Contains(_deathSpiral))
		{
			_deathSpiral.enabled = true;
		}
		else
		{
			_ability.AddAbility(_deathSpiral);
			_deathSpiral.enabled = true;
		}
	}

	public override void Exit()
	{
		if (_ability.Abilities.Contains(_deathSpiral))
		{
			_ability.RemoveAbility(_deathSpiral);
			_deathSpiral.enabled = false;
		}
		else
		{
			_deathSpiral.enabled = false;
			//ability.RemoveAbility(iceShard);
		}
	}
}
