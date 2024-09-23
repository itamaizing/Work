using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CircularFrosting : Skill
{
	//[SerializeField] private CircularFrostingObject _circle;
	[SerializeField] private Character _playerLinks;
	//[SerializeField] private FrostingFrozenTalant _talant;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;

	private float _baseDuration = 2;
	private float _duration = 2;
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
		CreateSmoke();
		yield return null;
	}

	protected override void ClearData()
	{
		
	}

	protected override IEnumerator PrepareJob()
	{
		yield return null;
	}

	private void CreateSmoke()
	{
		Collider2D[] enemyDetected = Physics2D.OverlapCircleAll(transform.position, Radius);
		if (_energy.CurrentValue >= 30)
		{
			_duration = _baseDuration + 3;
			_energy.CmdUse(30);
		}
		else
		{
			_duration = _baseDuration + _energy.CurrentValue / 10;
			_energy.CmdUse(_energy.CurrentValue);
		}
		foreach (var enemy in enemyDetected) 
		{
			//Debug.Log(enemy);
			if (enemy.TryGetComponent<Character>(out var enemyCharacter))
			{
				if (enemyCharacter != _playerLinks)
				{
					_seriesOfStrikes.MakeHit(enemyCharacter, AbilityForm.Magic, 1, 0);
					CmdAdd(enemy.gameObject);
					//enemyCharacter.CharacterState.CmdAddState(States.Frosting, _duration, 0, _playerLinks.gameObject, name);
				}
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

	[Command]
	private void CmdAdd(GameObject enemy)
	{
		Character enemyCharacter = enemy.GetComponent<Character>();
		enemyCharacter.CharacterState.AddState(States.Frosting, _duration, 0, _playerLinks.gameObject, name);
	}
}
