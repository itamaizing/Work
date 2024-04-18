using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcePuddleObject : MonoBehaviour
{
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] GameObject _hitEffect;

	[HideInInspector] public GameObject dad;
	[HideInInspector] public EnergyPlayer energyPlayer;
	[HideInInspector] public HealthPlayer healthPlayer;
	[HideInInspector] public float timeToDestroy = 3;
	/*
	 * timer to destroy
	 * buff player
	 * */
	private void Start()
	{
		int timeToAdd = (int)energyPlayer.Energy / 5;
		if (timeToAdd > 4)
			timeToAdd = 4;

		timeToDestroy += timeToAdd;
		energyPlayer.UseEnergy(timeToAdd * 5) ;
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
		//damage, freez etc
		if (collision.TryGetComponent<CharacterState>(out var target) && energyPlayer != null)
		{
			target.energy = dad.GetComponent<EnergyPlayer>();
			target.ChangeState(new FrozenState());
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
		healthPlayer.SetBoostRegen2(0);
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
