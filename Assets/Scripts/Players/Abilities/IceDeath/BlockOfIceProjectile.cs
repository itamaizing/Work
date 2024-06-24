using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockOfIceProjectile : Projectiles
{
	private Vector2 startPos;

	public void Init(GameObject dad)
	{
		_dad = dad.GetComponent<Character>();
		Debug.Log("bullet");
		_initialized = true;
	}
	private void Awake()
	{
		Debug.Log("bullet");
		startPos = transform.position;
		_rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
	}

	private void Update()
	{
		if (!_initialized) return;
		//_spriteRenderer.DOFade(0, 1);
		if (Vector2.Distance(transform.position, startPos) > _distance * GlobalVariable.cellSize)
		{
			Explode();
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (!_initialized || _dad == null) return;
		if (collision.gameObject == _dad.gameObject || collision.CompareTag("Ability"))
			return;
		//damage, freez etc
		if (collision.TryGetComponent<Character>(out var target))
		{
			//float duration = 1 + dad.Stamina.Value / 20;
			float duration = 9;
			//target.CharacterState.energy = dad.Stamina;
			float curDamage = 20 + Random.Range(0, 10);
			Energy energyLink = (Energy)_dad.Stamina;
			if (target.CharacterState.CheckForState(States.Frozen))
			{
				curDamage *= 1.4f;
			}
			energyLink.SumDamageMake(curDamage);
			target.Health.TryTakeDamage(curDamage, DamageType.Physical, AttackRangeType.RangeAttack);
			//target.CharacterState.AddState(new Cooling(), duration, 0, States.Cooling);
			target.CharacterState.CmdAddState(States.Cooling, duration, 0);

			//dad.Stamina.Use(duration * 20);
			//damage
			GetComponent<Collider2D>().enabled = false;
		}
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
