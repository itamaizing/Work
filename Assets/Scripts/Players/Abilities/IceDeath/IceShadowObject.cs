using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceShadowObject : MonoBehaviour
{
	[HideInInspector] public GameObject dad;
	[HideInInspector] public EnergyPlayer energyPlayer;
	[HideInInspector] public HealthPlayer healthPlayer;
	[HideInInspector] public float timeToDestroy = 2;
	[HideInInspector] public float timeToDestroyAlive = 10;

	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] GameObject _hitEffect;

	private Coroutine _destroyObj;
	private float _hp = 10;
	private bool _isAlive = false;
	/*
	 * timer to destroy
	 * buff player
	 * */
	private void Start()
	{
		float timeToAdd = energyPlayer.Value / 20;
		timeToDestroy += timeToAdd;
		energyPlayer.UseAllEnergy();
		_destroyObj = StartCoroutine(DestroyShadow());
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject == dad && healthPlayer != null)
		{
			healthPlayer.SetBoostRegen(0);
			return;
		}
	}
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject == dad)
		{
			healthPlayer.SetBoostRegen(0.01f);
		}
		if (collision.TryGetComponent<PlayerLinks>(out var target) && energyPlayer != null && collision.gameObject !=dad)
		{
			float duration = 2 + energyPlayer.Value / 20;
			//target.CharacterState.energy = energyPlayer;
			energyPlayer.UseAllEnergy();

			target.CharacterState.AddState(new FrozenState(), duration, 0);
			energyPlayer.Use(energyPlayer.Value);
			GetComponent<Collider2D>().enabled = false;
			Destroy(gameObject);
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
		healthPlayer.SetBoostRegen(0);
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
}
