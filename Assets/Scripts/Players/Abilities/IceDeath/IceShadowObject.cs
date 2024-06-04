using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class IceShadowObject : MonoBehaviour
{
	[HideInInspector] public GameObject dad;
	[FormerlySerializedAs("energyPlayer")] [HideInInspector] public Energy energy;
	[FormerlySerializedAs("healthPlayer")] [HideInInspector] public HealthComponent healthComponent;
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
		float timeToAdd = energy.Value / 20;
		timeToDestroy += timeToAdd;
		energy.UseAllEnergy();
		_destroyObj = StartCoroutine(DestroyShadow());
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject == dad && healthComponent != null)
		{
			healthComponent.SetBoostRegen(0);
			return;
		}
	}
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject == dad)
		{
			healthComponent.SetBoostRegen(0.01f);
		}
		if (collision.TryGetComponent<Character>(out var target) && energy != null && collision.gameObject !=dad)
		{
			float duration = 2 + energy.Value / 20;
			//target.CharacterState.energy = energy;
			energy.UseAllEnergy();

			target.CharacterState.AddState(new FrozenState());
			energy.Use(energy.Value);
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
		healthComponent.SetBoostRegen(0);
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
