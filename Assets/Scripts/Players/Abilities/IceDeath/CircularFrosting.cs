using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircularFrosting : Skill
{
	//[SerializeField] private CircularFrostingObject _circle;
	[SerializeField] private Character _links;
	//[SerializeField] private FrostingFrozenTalant _talant;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;

	private float _baseDuration = 2;
	private float _duration;

	protected override bool IsCanCast => true;


	protected override IEnumerator CastJob()
	{
		throw new System.NotImplementedException();
	}

	protected override void ClearData()
	{
		throw new System.NotImplementedException();
	}

	protected override IEnumerator PrepareJob()
	{
		throw new System.NotImplementedException();
	}

	[Command]
	private void CreateSmoke()
	{
		Collider2D[] enemyDetected = Physics2D.OverlapCircleAll(transform.position, Radius);
		if (_links.Stamina.CurrentValue >= 30)
		{
			_duration = _baseDuration + 3;
			_links.Stamina.TryUse(30);
		}
		else
		{
			_duration = _baseDuration + _links.Stamina.CurrentValue / 10;
			_links.Stamina.TryUse(_links.Stamina.CurrentValue);
		}
		foreach (var enemy in enemyDetected) 
		{
			if (enemy.TryGetComponent<Character>(out var enemyCharacter))
			{
				_seriesOfStrikes.MakeHit(enemyCharacter, AbilityForm.Magic, 1, 0);
				//enemyCharacter.CharacterState.AddState(new FrostingState(), _duration, 0, States.Frosting);
				enemyCharacter.CharacterState.CmdAddState(States.Frosting, _duration, 0, _links.gameObject, name);
				/*if (_talant != null)
				{
					if (_talant.IsActive)
					{
						enemyCharacter.CharacterState.CmdAddState(States.Frozen, _duration, 0);
						//enemyCharacter.CharacterState.AddState(new FrozenState(), _duration, 0, States.Frozen);
					}
				}*/
			}
		}
		//var smoke = Instantiate(_circle, transform);
		//smoke.dad = _links;
		//_canCast = false;
	}
}
