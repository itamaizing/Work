using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class IcePuddleObject : Projectiles
{
	[HideInInspector] public FrostingFrozenTalant talant;

	[FormerlySerializedAs("energyPlayer")]  private Energy _energy;
	[FormerlySerializedAs("healthPlayer")]  private HealthComponent _healthComponent;
	//[SerializeField] private Rigidbody2D _rb;

	private float _timeToDestroy = 0;
	private List<CharacterState> _enemies = new List<CharacterState>();
	/*
	 * buff player
	 * */
	public override void Init(Character dad, float timeToDestroy, bool lastHit)
	{
		_dad = dad;
		_initialized = true;
		_lastHit = lastHit;
		_energy = (Energy)_dad.Stamina;
		_healthComponent = _dad.Health;
		_timeToDestroy += timeToDestroy;
		if(_lastHit)
		{
			transform.localScale = Vector3.one * 1.7f;
		}

		StartCoroutine(DestroyShadow());
		StartCoroutine(StartFade());
	}
	private void Start()
	{
		_spriteRenderer.DOFade(1, 1);
	}


	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject == _dad.gameObject && _healthComponent != null)
		{
			_healthComponent.SetBoostRegen2(0);
			return;
		}		
	}
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if(!_initialized) return;
		if (collision.gameObject == _dad.gameObject)
		{
			_healthComponent.SetBoostRegen2(0.01f);
			return;
		}
		if (collision.TryGetComponent<Character>(out var target) && _energy != null && collision.gameObject != _dad.gameObject)
		{
			float duration = 3;
			//target.CharacterState.energy = energy;
			if(_energy.Value/5 > 4) 
			{
				duration += 4;
				_energy.Use(20);
			}
			else
			{
				duration += _energy.Value / 5;
				_energy.UseAllEnergy();
			}
			//target.CharacterState.AddState(new FrostingState(), duration,0,States.Frosting);
			target.CharacterState.CmdAddState(States.Frosting, duration, 0);
			if (talant != null)
			{
				if (talant.IsActive)
				{
					//target.CharacterState.AddState(new FrozenState(), duration, 0, States.Frozen);
					target.CharacterState.CmdAddState(States.Frozen, duration, 0);
				}
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
		_healthComponent.SetBoostRegen2(0);
		foreach (var target in _enemies)
		{
			target.RemoveState(States.Frosting); 
			_enemies.Remove(target);
		}
		Destroy(gameObject);
	}

	private IEnumerator DestroyShadow()
	{
		yield return new WaitForSeconds(_timeToDestroy);
		Destroy(gameObject);
		//turn off energy boost
		//destroy
	}
	private IEnumerator StartFade()
	{
		yield return new WaitForSeconds(_timeToDestroy-2);
		_spriteRenderer.DOFade(0, 2);
		//turn off energy boost
		//destroy
	}
}
