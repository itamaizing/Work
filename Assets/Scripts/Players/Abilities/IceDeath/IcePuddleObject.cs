using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IcePuddleObject : MonoBehaviour
{
	[HideInInspector] public GameObject dad;
	[HideInInspector] public EnergyPlayer energyPlayer;
	[HideInInspector] public HealthPlayer healthPlayer;
	[HideInInspector] public float timeToDestroy = 3;

	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] GameObject _hitEffect;

	private List<CharacterState> _enemies = new List<CharacterState>();

	/*
	 * timer to destroy
	 * buff player
	 * */
	private void Start()
	{
		//energyPlayer.test();
		int timeToAdd = (int)energyPlayer.Value / 5;
		if (timeToAdd > 4)
			timeToAdd = 4;

		timeToDestroy += timeToAdd;
		energyPlayer.Use(timeToAdd * 5) ;
		StartCoroutine(DestroyShadow());
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject == dad && healthPlayer != null)
		{
			healthPlayer.SetBoostRegen2(0);
			return;
		}		
	}
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject == dad)
		{
			healthPlayer.SetBoostRegen2(0.01f);
			return;
		}
		if (collision.TryGetComponent<PlayerLinks>(out var target) && energyPlayer != null && collision.gameObject != dad)
		{
			float duration = 3;
			//target.CharacterState.energy = energyPlayer;
			if(energyPlayer.Value/5 > 4) 
			{
				duration += 4;
				energyPlayer.Use(20);
			}
			else
			{
				duration += energyPlayer.Value / 5;
				energyPlayer.UseAllEnergy();
			}
			target.CharacterState.AddState(new FrostingState(), duration, 30);
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
		healthPlayer.SetBoostRegen2(0);
		foreach (var target in _enemies)
		{
			target.AddState(new DefaultState());
			_enemies.Remove(target);
		}
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
