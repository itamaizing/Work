using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingSmoke : Ability
{
	[SerializeField] private HealingSmokeObject _smoke;
	[SerializeField] private Character _links;
	private float _cooldownTimer = 0;
	private bool _canCast = true;
	//how to mark ally

	private void Update()
	{
		if (_canCast) return;
		Timer();
	}
	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	protected override void Cast()
	{
		if (_canCast)
		{
			PayCost();
			CreateSmoke();
		}
	}
	private void CreateSmoke()
	{
		var smoke = Instantiate(_smoke, transform);
		smoke.dad = _links;
		_canCast = false;
	}

	private void Timer()
	{
		_cooldownTimer += Time.deltaTime;
		if( _cooldownTimer >= _cooldown ) 
		{
			_canCast=true;
			_cooldownTimer=0;
		}
	}
}
