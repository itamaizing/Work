using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;

public class IceShadowObject : Projectiles
{
	//[HideInInspector] public EnergyPlayer energyPlayer;
	[HideInInspector] public float timeToDestroy = 20;
	[HideInInspector] public float timeToDestroyAlive = 10;


	private HealthComponent _healthPlayer;
	private Coroutine _destroyObj;
	private bool _isAlive = false;
	/*
	 * timer to destroy
	 * buff player
	 * */
	
	public override void Init(Character dad, float energy, bool lastHit)
	{
		_dad = dad;
		_energyDad = energy;
		_healthPlayer = _dad.Health;
		_initialized = true;
		_lastHit = lastHit;

		float timeToAdd = energy / 20;
		timeToDestroy += timeToAdd;
		Debug.Log("bullet init  " + _dad.name);
	}

	private void Start()
	{
		_destroyObj = StartCoroutine(DestroyShadow());
	}
	private void Update()
	{
		if (_initialized) { Debug.Log("SEEEEEEEEEEEEEEEEEEEEEEEEEET"); }
	}
	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject == _dad && _healthPlayer != null)
		{
			_healthPlayer.SetBoostRegen(0);
			return;
		}
	}
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if(_dad == null) return;
		if (collision.gameObject == _dad.gameObject)
		{
			_healthPlayer.SetBoostRegen(0.01f);
		}
		if (collision.TryGetComponent<Character>(out var target) && collision.gameObject !=_dad.gameObject)
		{
			float duration = 2 + _energyDad / 20;

			target.CharacterState.CmdAddState(States.Frozen, duration, 0);
			//GetComponent<Collider2D>().enabled = false;
			//Destroy(gameObject);
			if(_lastHit)
			{
				Collider2D[] enemyDetected = Physics2D.OverlapCircleAll(transform.position, 3);
				foreach (var enemy in enemyDetected) 
				{
					if (enemy.TryGetComponent<Character>(out var newTatget) && collision.gameObject != _dad.gameObject)
					{
						newTatget.CharacterState.CmdAddState(States.Frozen, duration, 0);
					}


				}
			}
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
		_healthPlayer.SetBoostRegen(0);
		Destroy(gameObject);
	}

	private IEnumerator DestroyShadow()
	{
		yield return new WaitForSeconds(timeToDestroy);
		if(!_isAlive)
			Destroy(gameObject);
		//turn off energy boost
		//destroy	
	}
	private IEnumerator DestroyAliveShadow()
	{
		yield return new WaitForSeconds(timeToDestroyAlive);
			Destroy(gameObject);
		//turn off energy boost
		//destroy	
	}

	public void SetAlive()
	{
		_isAlive = true;
		StartCoroutine(DestroyAliveShadow());
		//_destroyObj.
	}

	/*public void SetEnergy(float value)
	{
		float timeToAdd = value / 20;
		timeToDestroy += timeToAdd;
		_energyValue = value;
	}*/
}
