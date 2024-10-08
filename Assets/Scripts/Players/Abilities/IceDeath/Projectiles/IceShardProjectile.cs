using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Mirror;

public class IceShardProjectile : Projectiles
{
	private Vector2 _startPos;
	private bool _talentPlague = false;
	private bool _talentChragesPlague = false;

	private void Update()
	{
		_spriteRenderer.DOFade(0, 1);
		if (Vector2.Distance(transform.position, _startPos) > _distance * GlobalVariable.cellSize)
		{
			Explode();
		}
	}

	[Server]
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (_dad == null) return;
		if (collision.gameObject == _dad.gameObject || collision.CompareTag("Ability"))
			return;
		//damage, freez etc
		if (collision.TryGetComponent<Character>(out var target))
		{
			float duration = 1 + _energyDad / 20;
			float curDamage = 3 + Random.Range(0, 3);
			if (target.CharacterState.CheckForState(States.Frozen) && Random.Range(0, 100) < 15)
			{
				curDamage *= 2.2f;
			}
			_energy.SumDamageMake(curDamage);
			Damage damage = new Damage
			{
				Value = curDamage,
				Type = DamageType.Physical,
				PhysicAttackType = AttackRangeType.RangeAttack,
			};
			//_skill.CmdApplyDamage(damage, target.gameObject);
			target.Health.TryTakeDamage(ref damage, _skill);

			//target.Health.TryTakeDamage(curDamage, DamageType.Physical, AttackRangeType.RangeAttack);
			target.CharacterState.AddState(States.Frozen, duration, 30, _dad.gameObject, _skill.name);
			if(_talentPlague)
			{
				target.CharacterState.AddState(States.Plague, 5, 0, _dad.gameObject, _skill.name);
			}
			if(_talentChragesPlague)
			{
				//target.CharacterState.personWhoShoted = _dad;
			}
			//dad.Stamina.Use(duration * 20);
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
	public void Talents(bool plague, bool charges)
	{
		_talentPlague = plague;
		_talentChragesPlague = charges;
	}
	
}
