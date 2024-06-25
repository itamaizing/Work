using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CounterSpell : Ability
{
	[SerializeField] private Character _links;
	private float _cooldownTimer = 0;
	private bool _canCast = true;
	private float _timer = 0;
	private float _baseDuration = 2;
	private float _duration = 10;
	protected override void Cast()
	{
		Collider2D[] enemyDetected = Physics2D.OverlapCircleAll(transform.position, Radius);
		/*if (_links.Stamina.Value >= 30)
		{
			_duration = _baseDuration + 3;
			_links.Stamina.Use(30);
		}
		else
		{
			_duration = _baseDuration + _links.Stamina.Value / 10;
			_links.Stamina.Use(_links.Stamina.Value);
		}*/
		
		foreach (var enemy in enemyDetected)
		{
			Debug.Log("TEST");
			if (enemy.TryGetComponent<Character>(out var enemyCharacter))
			{
				//enemyCharacter.CharacterState.AddState(new AbilitySchoolDebuff(), _duration, 0, States.SchoolDebuff, Schools.Fire);
				enemyCharacter.CharacterState.CmdAddState(States.SchoolDebuff, _duration, 0, Schools.Fire);
			}
		}
	}

	protected override void Cancel()
	{
		
	}

}
