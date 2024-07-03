using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Absorption : Ability
{
	[SerializeField] private Character _player;

	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	protected override void Cast()
	{

	}

	private void Action(IceShadowObject body)
	{
		if(body.IsAlive) 
		{
			float regen = 0.1f * body.HP + 0.05f * _player.Stamina.Value/10;
			_player.Stamina.Use(_player.Stamina.Value);
			_player.Health.AddHeal(regen);
			body.Explode();
		}
	}
}
