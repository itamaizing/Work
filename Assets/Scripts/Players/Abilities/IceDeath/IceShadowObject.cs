using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceShadowObject : MonoBehaviour
{
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] GameObject _hitEffect;

	[HideInInspector] public GameObject dad;
	[HideInInspector] public EnergyPlayer energyPlayer;
	[HideInInspector] public float timeToDestroy = 2;
	/*
	 * timer to destroy
	 * buff player
	 * */
	private void Start()
	{
		timeToDestroy += energyPlayer.Energy/20;
		StartCoroutine(DestroyShadow());
	}

	private void OnTriggerStay2D(Collider2D collision)
	{
		if (collision.gameObject == dad && energyPlayer != null)
		{
			//energy recharge
		}
	}
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject == dad)
			return;
		//damage, freez etc
		if (collision.TryGetComponent<CharacterState>(out var target))
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
