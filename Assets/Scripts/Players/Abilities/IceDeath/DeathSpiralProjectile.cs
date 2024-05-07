using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSpiralProjectile : MonoBehaviour
{
	[HideInInspector] public GameObject dad;

	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] GameObject _hitEffect;
	[SerializeField] private float _force;
	[SerializeField] private float _distance = 5;

	private Vector2 startPos;

	private void Awake()
	{
		startPos = transform.position;

		_rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
	}

	private void Update()
	{
		if (Vector2.Distance(transform.position, startPos) > _distance * GlobalVariable.cellSize)
		{
			Explode();
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject == dad || collision.CompareTag("Ability"))
			return;
		//damage, freez etc
		if (collision.TryGetComponent<HealthPlayer>(out var target))
		{
			//damage
		}

		//if collision == ice puddle or ice shadow
		Explode();
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
}
