using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceShadowObject : MonoBehaviour
{
	[HideInInspector] public GameObject dad;
	[HideInInspector] public EnergyPlayer energyPlayer;
	[HideInInspector] public HealthPlayer healthPlayer;
	[HideInInspector] public float timeToDestroy = 2;

	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] GameObject _hitEffect;

	/*
	 * timer to destroy
	 * buff player
	 * */
	private void Start()
	{
		int timeToAdd = (int)energyPlayer.Value / 20;
		timeToDestroy += timeToAdd;
		energyPlayer.Use(timeToAdd*20);
		StartCoroutine(DestroyShadow());
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
		//damage, freez etc
		if (collision.TryGetComponent<CharacterState>(out var target) && energyPlayer != null && collision.gameObject !=dad)
		{
			target.energy = dad.GetComponent<EnergyPlayer>();
			target.AddState(new FrozenState());
			GetComponent<Collider2D>().enabled = false;
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
		Destroy(gameObject);
		//turn off energy boost
		//destroy
			

	}
}
