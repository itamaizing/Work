using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class IcePuddleObject : Projectiles
{
	[FormerlySerializedAs("healthPlayer")]  private Health _healthComponent;

	private float _timeToDestroy = 0;
	private float _curEvade = 0;
	private bool _talentEvadeDadBoost = false;
	private bool _talentFrostingFrozen = false;
	private List<CharacterState> _enemies = new List<CharacterState>();
	private List<EnemyToState> _targets;
	/*
	 * buff player
	 * */
	public override void Init(HeroComponent dad, float timeToDestroy, bool lastHit, Skill skill)
	{
		_dad = dad;
		_skill = skill;
		_initialized = true;
		_lastHit = lastHit;
		_healthComponent = _dad.Health;
		_timeToDestroy += timeToDestroy;
		if(_lastHit)
		{
			transform.localScale = Vector3.one * 1.7f;
		}
		for (int i = 0; i < _dad.Resources.Count; i++)
		{
			if (_dad.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_dad.Resources[i];
			}
		}


		StartCoroutine(DestroyPuddle());
		StartCoroutine(StartFade());
	}

	private void Update()
	{
		if (_targets.Count <= 0) return;

		for(int i = 0; i < _targets.Count; i++)
		{
			_targets[i].duration -= Time.deltaTime;
			if (_targets[i].duration < 0 )
			{
				_targets[i].enemy.CharacterState.AddState(States.Frosting, _targets[i].duration, 0, _dad.gameObject, _skill.name);
				_targets.Remove(_targets[i]);
			}
		}
	}

	public void SetTalents(bool talentEvadeDadBoost, bool talentFrostingFrozen)
	{
		_talentEvadeDadBoost= talentEvadeDadBoost;
		_talentFrostingFrozen= talentFrostingFrozen;
	}

	private void Start()
	{
		_spriteRenderer.DOFade(1, 1);
	}


	private void OnTriggerExit(Collider collision)
	{
		if (collision.gameObject == _dad.gameObject && _healthComponent != null)
		{
			//Debug.LogError("fix");
			//_healthComponent.SetBoostRegen2(0);
			return;
		}
		if (collision.TryGetComponent<Character>(out var target) && collision.gameObject != _dad.gameObject)
		{
			for(int i = 0; i < _targets.Count; i++) 
			{
				if (_targets[i].enemy == target)
				{
					_targets.Remove(_targets[i]);
				}
			}

			if (_talentEvadeDadBoost)
			{
				//Debug.LogError("fix");
				_curEvade = -3;
				_dad.Health.SetEvadeAll(-3);
			}
		}
	}

	[Server]
	private void OnTriggerEnter(Collider collision)
	{
		if(!_initialized) return;
		//Debug.Log(collision.name);
		if (collision.gameObject == _dad.gameObject)
		{
			//Debug.LogError("fix");
			//_healthComponent.SetBoostRegen2(0.01f);
			return;
		}
		if (collision.TryGetComponent<Character>(out var target) && _energy != null)
		{
			Debug.Log(target.name);
			float duration = 3;
			//target.CharacterState.energy = energy;
			if (_energy.CurrentValue / 5 > 4)
			{
				duration += 4;
				_energy.TryUse(20);
			}
			else
			{
				duration += _energy.CurrentValue / 5;
				_energy.UseAllEnergy();
			}

			EnemyToState enemy = new EnemyToState();
			enemy.enemy = target;
			enemy.duration = duration;
			//target.CharacterState.AddState(States.Frosting, duration, 0, _dad.gameObject, _skill.name);

			if (_talentFrostingFrozen)
			{
				target.CharacterState.AddState(States.Frozen, duration, 0, _dad.gameObject, _skill.name);
			}
			if (_talentEvadeDadBoost)
			{
				//Debug.LogError("fix");
				_curEvade = 3;
				_dad.Health.SetEvadeAll(3);
			}
			_enemies.Add(target.CharacterState);
		}
		//Explode();
	}
	private void Explode()
	{
		if (_hitEffect != null)
		{
			GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
			Destroy(hitEffect, 5f);
		}
		Debug.LogError("fix");
		//_healthComponent.SetBoostRegen2(0);
		foreach (var target in _enemies)
		{
			target.CmdRemoveState(States.Frosting); 
			_enemies.Remove(target);
		}
		Destroy(gameObject);
	}

	private IEnumerator DestroyPuddle()
	{
		yield return new WaitForSeconds(_timeToDestroy);
		_dad.Health.SetEvadeAll(-_curEvade);
		Destroy(gameObject);
		//turn off energy boost
		//destroy
	}
	private IEnumerator StartFade()
	{
		yield return new WaitForSeconds(_timeToDestroy-2);
		//_spriteRenderer.DOFade(0, 2);
		//turn off energy boost
		//destroy
	}

	private IEnumerator AddStateToEnemy(Character enemy, float duration)
	{
		yield return new WaitForSeconds(1);
		enemy.CharacterState.AddState(States.Frosting, duration, 0, _dad.gameObject, _skill.name);
	}
}

public class EnemyToState
{
	public Character enemy;
	public float time = 1;
	public float duration = 1;
}