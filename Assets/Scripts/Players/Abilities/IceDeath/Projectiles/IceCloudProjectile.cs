using DG.Tweening;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class IceCloudProjectile : Projectiles
{	
	private Vector2 _startPos;
	private Damage _damage;
	private bool _boostDmg;
	private float _curDamage;

	private void Start()
	{
		_startPos = transform.position;
		_curDamage = 500 + _energyDad / 4;
		_damage = new Damage
		{
			Value = _curDamage,
			Type = DamageType.Physical,
		};
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

		if(collision.TryGetComponent<IDamageable>(out var damageable))
		{
			if (damageable is Character target)
			{
				Debug.Log(collision.name);
				target.CharacterState.AddState(States.Plague, 40, 0, _dad.gameObject, _skill.Name);
				//target.CharacterState.personWhoShoted = _dad;

				float duration = 1 + _energyDad / 20;

				if (target.CharacterState.CheckForState(States.Frozen) && _boostDmg)
				{
					_curDamage *= 1.4f;
					Debug.Log("NEW DAMAGE");
				}

				_energy.SumDamageMake(_curDamage);
				
				//_skill.CmdApplyDamage(damage, target.gameObject);
				target.Health.TryTakeDamage(ref _damage, _skill);


				target.CharacterState.AddState(States.Frozen, duration, 30, _dad.gameObject, _skill.name);

				//talents???
				if (_dad.Health.EvadeMagDamage >= 20)
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
			else
			{
				damageable.TryTakeDamage(ref _damage, _skill);
				if (_damage.Value <= 0)
				{
					Explode();
				}
				return;
			}
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
