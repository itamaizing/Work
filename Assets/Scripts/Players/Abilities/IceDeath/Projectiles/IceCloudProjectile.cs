using DG.Tweening;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class IceCloudProjectile : Projectiles
{	
	private Vector2 _startPos;
	private bool _boostDmg;

	private void Start()
	{
		_startPos = transform.position;
	}

	private void Update()
	{
        _spriteRenderer.DOFade(0, 1);
		if(Vector2.Distance(transform.position, _startPos) > _distance * GlobalVariable.cellSize)
		{
			Explode();
		}
	}

	[Server]
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (_dad == null) return;
		if (collision.gameObject == _dad.gameObject)
			return;

		if(collision.TryGetComponent<Character>(out var target))
		{
			Debug.Log(collision.name);
			target.CharacterState.AddState(States.Plague, 40, 0, _dad.gameObject, _skill.Name);
			//target.CharacterState.personWhoShoted = _dad;

			float duration = 1 + _energyDad / 20;
			float curDamage = 10 + _energyDad / 4;

			if (target.CharacterState.CheckForState(States.Frozen) && _boostDmg)
			{
				curDamage *= 1.4f;
				Debug.Log("NEW DAMAGE");
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


			target.CharacterState.AddState(States.Frozen, duration, 30, _dad.gameObject, _skill.name);

			//talents???
			if (_dad.Health.EvadeMagDamage >=20)
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
			Explode();
		}
		
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

	public void TalentBoostDmg(bool value)
	{
		_boostDmg = value;
	}
}
