using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircularFrosting : Skill
{
	//[SerializeField] private CircularFrostingObject _circle;
	[SerializeField] private Character _playerLinks;
	//[SerializeField] private FrostingFrozenTalant _talant;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;

	private float _baseDuration = 2;
	private float _duration;
	private Energy _energy;

	protected override bool IsCanCast => true;

	private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
		}

	}

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
		if (_energy.CurrentValue >= 30)
		{
			_duration = _baseDuration + 3;
			_energy.TryUse(30);
		}
		else
		{
			_duration = _baseDuration + _energy.CurrentValue / 10;
			_energy.TryUse(_energy.CurrentValue);
		}
		foreach (var enemy in enemyDetected) 
		{
			if (enemy.TryGetComponent<Character>(out var enemyCharacter))
			{
				_seriesOfStrikes.MakeHit(enemyCharacter, AbilityForm.Magic, 1, 0);
				//enemyCharacter.CharacterState.AddState(new FrostingState(), _duration, 0, States.Frosting);
				enemyCharacter.CharacterState.CmdAddState(States.Frosting, _duration, 0, _playerLinks.gameObject, name);
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
