using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSpiralProjectile : Projectiles
{
	private Vector2 startPos;

	private void Awake()
	{
		Debug.Log("has spawned");
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
		if (_dad == null) return;
		if (collision.gameObject == _dad)
			return;
		//damage, freez etc
		if (collision.TryGetComponent<Character>(out var target))
		{
			target.Health.TryTakeDamage(20, DamageType.Physical, AttackRangeType.RangeAttack);
			//damage
		}
		if(collision.TryGetComponent<IceShadowObject>(out var shadow))
		{
			shadow.SetAlive();
			Debug.Log(shadow.name + " become alive");
		}

		//if collision == ice puddle or ice shadow
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
}
