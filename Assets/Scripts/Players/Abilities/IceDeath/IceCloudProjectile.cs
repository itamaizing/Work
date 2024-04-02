using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceCloudProjectile : MonoBehaviour
{
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] GameObject _hitEffect;
	[SerializeField] private float force;

	private void Awake()
	{
		_rb.AddForce(transform.up * force, ForceMode2D.Impulse);
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		//damage, freez etc


		GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
		Destroy(hitEffect, 5f);
		Destroy(gameObject);
	}
}
