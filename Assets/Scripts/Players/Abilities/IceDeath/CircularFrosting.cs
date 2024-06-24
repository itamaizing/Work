using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircularFrosting : Ability
{
	//[SerializeField] private CircularFrostingObject _circle;
	[SerializeField] private Character _links;
	[SerializeField] private FrostingFrozenTalant _talant;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	private float _cooldownTimer = 0;
	private bool _canCast = true;
	private float _timer = 0;
	private float _baseDuration = 2;
	private float _duration;
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
			//PayCost();
			CreateSmoke();
		}
	}

	[Command]
	private void CreateSmoke()
	{
		Collider2D[] enemyDetected = Physics2D.OverlapCircleAll(transform.position, Radius);
		if (_links.Stamina.Value >= 30)
		{
			_duration = _baseDuration + 3;
			_links.Stamina.Use(30);
		}
		else
		{
			_duration = _baseDuration + _links.Stamina.Value / 10;
			_links.Stamina.Use(_links.Stamina.Value);
		}
		foreach (var enemy in enemyDetected) 
		{
			if (enemy.TryGetComponent<Character>(out var enemyCharacter))
			{
				_seriesOfStrikes.MakeHit(enemyCharacter, AbilityForm.Magic, 1);
				//enemyCharacter.CharacterState.AddState(new FrostingState(), _duration, 0, States.Frosting);
				enemyCharacter.CharacterState.CmdAddState(States.Frosting, _duration, 0);
				if (_talant != null)
				{
					if (_talant.IsActive)
					{
						enemyCharacter.CharacterState.CmdAddState(States.Frozen, _duration, 0);
						//enemyCharacter.CharacterState.AddState(new FrozenState(), _duration, 0, States.Frozen);
					}
				}
			}
		}
		//var smoke = Instantiate(_circle, transform);
		//smoke.dad = _links;
		//_canCast = false;
	}

	private void Timer()
	{
		_cooldownTimer += Time.deltaTime;
		if (_cooldownTimer >= _cooldown)
		{
			_canCast = true;
			_cooldownTimer = 0;
		}
	}
}
