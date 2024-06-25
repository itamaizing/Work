using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class IceCloudProjectile : Projectiles
{	
	private Vector2 startPos;


	private void Awake()
	{
		//_dad = dadGm.GetComponent<Character>();
		startPos = transform.position;
		_rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
	}

	private void Update()
	{
		//if (!_initialized) return;

        _spriteRenderer.DOFade(0, 1);
		if(Vector2.Distance(transform.position, startPos) > _distance * GlobalVariable.cellSize)
		{
			Explode();
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (_dad == null) return;
		if (collision.gameObject == _dad.gameObject || collision.CompareTag("Ability"))
			return;
		//damage, freez etc
		if(collision.TryGetComponent<Character>(out var target))
		{
			//float duration = 1 + dad.Stamina.Value / 20;
			float duration = 1 + _energyDad / 20;
			//target.CharacterState.energy = dad.Stamina;
			float curDamage = 10 + _energyDad / 4;
			Energy energyLink = (Energy)_dad.Stamina;
			if (target.CharacterState.CheckForState(States.Frozen))
			{
				curDamage *= 1.4f;
			}
			energyLink.SumDamageMake(curDamage);
			//target.Health.TakeDamage(curDamage, DamageType.Physical);
			target.Health.TryTakeDamage(curDamage, DamageType.Physical, AttackRangeType.RangeAttack);
			//target.CharacterState.AddState(new FrozenState(), duration, 30, States.Frozen);
			target.CharacterState.CmdAddState(States.Frozen, duration, 30);
			if(_dad.Health.GetEvadeMagic() >=20)
			{
				_dad.Health.SetEvadeMagic(5);
			}
			else
			{
				_dad.Health.SetEvadeMagic(20);
			}

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
