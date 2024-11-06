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
		_curDamage = 10 + _energyDad / 4;
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
	private void OnTriggerEnter(Collider collision)
	{
		if (_dad == null) return;
		if (collision.gameObject == _dad.gameObject)
			return;

		if(collision.TryGetComponent<IDamageable>(out var damageable))
		{
			if (collision.TryGetComponent<Character>(out var target) && target != _dad)
			//if (damageable is HeroComponent target)
			{
				
				//target.CharacterState.AddState(States.Plague, 40, 0, _dad.gameObject, _skill.Name);

				float duration = 100 + _energyDad / 20;

				if (target.CharacterState.CheckForState(States.Frozen) && _boostDmg)
				{
					_curDamage *= 1.4f;
					Debug.Log("NEW DAMAGE");
				}

				_energy.SumDamageMake(_curDamage);
				
				//_skill.CmdApplyDamage(_damage, target.gameObject);
				target.Health.TryTakeDamage(ref _damage, _skill);


				target.CharacterState.AddState(States.Frozen, duration, 0, _dad.gameObject, _skill.name);

				//talents???
				if (_dad.Health.ResistMagDamage >= 20)
				{
					_dad.Health.SetEvadeMagic(5);
				}
				else
				{
					_dad.Health.SetEvadeMagic(20);
				}

				//dad.Stamina.Use(duration * 20);
				//damage
				GetComponent<Collider>().enabled = false;
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
