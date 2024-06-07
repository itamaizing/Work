using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockOfIceProjectile : MonoBehaviour
{
	public float energyDad;
	[HideInInspector] public Character dad;

	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] GameObject _hitEffect;
	[SerializeField] SpriteRenderer _spriteRenderer;
	[SerializeField] private float _force;
	[SerializeField] private float _distance = 6;

	private Vector2 startPos;

	private void Awake()
	{
		Debug.Log("bullet");
		startPos = transform.position;
		_rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
	}

	private void Update()
	{
		//_spriteRenderer.DOFade(0, 1);
		if (Vector2.Distance(transform.position, startPos) > _distance * GlobalVariable.cellSize)
		{
			Explode();
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject == dad.gameObject || collision.CompareTag("Ability"))
			return;
		//damage, freez etc
		if (collision.TryGetComponent<Character>(out var target))
		{
			//float duration = 1 + dad.Stamina.Value / 20;
			float duration = 9;
			//target.CharacterState.energy = dad.Stamina;
			float curDamage = 20 + Random.Range(0, 10);
			Energy energyLink = (Energy)dad.Stamina;
			if (target.CharacterState.CheckForState(States.Frozen))
			{
				curDamage *= 1.4f;
			}
			energyLink.SumDamageMake(curDamage);
			target.Health.TakeDamage(curDamage, DamageType.Physical);
			target.CharacterState.AddState(new Cooling(), duration, 0, States.Cooling);

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
