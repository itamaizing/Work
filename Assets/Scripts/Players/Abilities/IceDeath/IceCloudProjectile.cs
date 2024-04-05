using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceCloudProjectile : MonoBehaviour
{
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] GameObject _hitEffect;
	[SerializeField] private float force;

	[HideInInspector]public GameObject dad;

	private void Awake()
	{
		_rb.AddForce(transform.up * force, ForceMode2D.Impulse);
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject == dad)
			return;
		//damage, freez etc
		if(collision.TryGetComponent<CharacterState>(out var target))
		{
			target.energy = dad.GetComponent<EnergyPlayer>();
			target.ChangeState(new FrozenState());
			GetComponent<Collider2D>().enabled = false;
		}

		if(_hitEffect != null)
		{
			GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
			Destroy(hitEffect, 5f);
		}		
		Destroy(gameObject);
	}
}
