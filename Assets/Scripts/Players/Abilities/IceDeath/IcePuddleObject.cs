using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class IcePuddleObject : MonoBehaviour
{
	[HideInInspector] public GameObject dad;
	[FormerlySerializedAs("energyPlayer")] [HideInInspector] public Energy energy;
	[FormerlySerializedAs("healthPlayer")] [HideInInspector] public HealthComponent healthComponent;
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
		//energy.test();
		int timeToAdd = (int)energy.Value / 5;
		if (timeToAdd > 4)
			timeToAdd = 4;

		timeToDestroy += timeToAdd;
		energy.Use(timeToAdd * 5) ;
		StartCoroutine(DestroyShadow());
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject == dad && healthComponent != null)
		{
			healthComponent.SetBoostRegen2(0);
			return;
		}		
	}
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject == dad)
		{
			healthComponent.SetBoostRegen2(0.01f);
			return;
		}
		if (collision.TryGetComponent<Character>(out var target) && energy != null && collision.gameObject != dad)
		{
			float duration = 3;
			//target.CharacterState.energy = energy;
			if(energy.Value/5 > 4) 
			{
				duration += 4;
				energy.Use(20);
			}
			else
			{
				duration += energy.Value / 5;
				energy.UseAllEnergy();
			}
			target.CharacterState.AddState(new FrostingState(),0,0,0); // TODO ADDVALUES
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
		healthComponent.SetBoostRegen2(0);
		foreach (var target in _enemies)
		{
			target.AddState(new DefaultState(),0,0,0); //TODO ADDVALUES
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
