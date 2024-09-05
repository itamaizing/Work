using Mirror;
using System.Collections;
using UnityEngine;

public class IceShadowObject : Projectiles
{
	//[HideInInspector] public EnergyPlayer energyPlayer;
	[HideInInspector] public float timeToDestroy = 30;
	[HideInInspector] public float timeToDestroyAlive = 30;

	private Health _healthPlayer;

	/*
	 * timer to destroy
	 * buff player
	 * */
	public override void Init(HeroComponent dad, float energy, bool lastHit, Skill skill)
	{
		_skill = skill;
		_dad = dad;
		_energyDad = energy;
		_healthPlayer = _dad.Health;
		_initialized = true;
		_lastHit = lastHit;

		float timeToAdd = energy / 20;
		timeToDestroy += timeToAdd;
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject == _dad && _healthPlayer != null)
		{
			//_healthPlayer.SetBoostRegen(0);
			Debug.LogError("setboost in hp has been deleted");

			return;
		}
	}
	//[Server]
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if(_dad == null) return;
		if (collision.gameObject == _dad.gameObject)
		{
			//_healthPlayer.SetBoostRegen(0.01f);
			//Debug.LogError("setboost in hp has been deleted");
		}
		if(collision.TryGetComponent<IcePuddleObject>(out var obj)) 
		{
			//attact speed increase
		}
		if (collision.TryGetComponent<Character>(out var target) && collision.gameObject !=_dad.gameObject)
		{
			float duration = 2 + _energyDad / 20;

			target.CharacterState.CmdAddState(States.Frozen, duration, 0, _dad.gameObject, _skill.name);
			//GetComponent<Collider2D>().enabled = false;
			//Destroy(gameObject);
			if(_lastHit)
			{
				Collider2D[] enemyDetected = Physics2D.OverlapCircleAll(transform.position, 3);
				foreach (var enemy in enemyDetected) 
				{
					if (enemy.TryGetComponent<Character>(out var newTatget) && collision.gameObject != _dad.gameObject)
					{
						newTatget.CharacterState.CmdAddState(States.Frozen, duration, 0, _dad.gameObject, _skill.name);
					}


				}
			}
		}
		//Explode();
	}

	public void Explode()
	{
		if (_hitEffect != null)
		{
			GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
			Destroy(hitEffect, 5f);
		}

		//_healthPlayer.SetBoostRegen(0);
		Debug.LogError("SetBoostRegen has been deleted");

		Destroy(gameObject);
	}

	private IEnumerator DestroyShadow()
	{
		yield return new WaitForSeconds(timeToDestroy);
		Destroy(gameObject);
		//turn off energy boost
		//destroy	
	}
}
