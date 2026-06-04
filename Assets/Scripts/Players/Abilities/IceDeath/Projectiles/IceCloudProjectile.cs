using DG.Tweening;
using Mirror;
using System.Collections;
using UnityEngine;

public class IceCloudProjectile : Projectiles
{	
	private Vector2 _startPos;
	private Damage _damage;
	private bool _boostDmg;
	private float _damagBase;
	private float _freezeDurationBase = 1f;
	private float _curFreezeDuration;
	private float _curDamage;
	private float _damageToExit = 1;
	private float _usedEnergy;

	private const float FrostEnergyCoolingBonusPerStack = 1f;
	private const float FrostEnergyFrostingBonusPerStack = 5f;
	private const float FrostEnergyFrozenBonusPerStack = 10f;

	private bool _isReflected;

	private void Start()
	{
		_startPos = transform.position;
		_curDamage = 10 + _usedEnergy / 5;

		_damage = new Damage
		{
			Value = _curDamage,
			Type = DamageType.Physical,
		};
	}

	private void Update()
	{
		if (!_initialized) return;

		if (!_isReflected) _spriteRenderer.DOFade(0, 1);
		//Debug.Log("Dist " + Vector2.Distance(transform.position, _startPos) + " Max dist " + _distance);
		if(Vector2.Distance(transform.position, _startPos) > _distance)
		{
			Explode();
		}
	}

	public void InitIceCloud(float usedEnergy, float damage)
	{
		_damagBase = damage;
		_usedEnergy = usedEnergy;
		_curDamage = _damagBase + _usedEnergy / 5f;
		_curFreezeDuration = _freezeDurationBase + _usedEnergy / 20f;
	}

	[Server]
	private void OnTriggerEnter(Collider collision)
	{
		if (!_initialized) return;
		if (_dad == null) return;

		if (!collision.TryGetComponent<Character>(out var target)) return;

		if (target.CharacterState.CheckForState(States.ReflectiveScales) &&	_damage.Type == DamageType.Magical)
		{
			if (_isReflected) return;

			target.CharacterState.RemoveState(States.ReflectiveScales);
			Reflect(target);
			return;
		}

		if (collision.gameObject == _dad.gameObject) return;

		if (!collision.TryGetComponent<IDamageable>(out var damageable))
			return;

		if (target != _dad)
		{
			float finalDamage = _curDamage;

			if (_boostDmg && target.CharacterState.CheckForState(States.Frozen))
			{
				finalDamage *= 1.4f;
			}

			_damage.Value = finalDamage;

			TargetRpcDamageMake(finalDamage);
			//target.Health.TryTakeDamage(ref _damage, _skill);
			_skill.ApplyDamage(_damage,target.gameObject);

            StartCoroutine(CrutchDelay(target, _curFreezeDuration));

            //target.CharacterState.AddState(States.Frozen, _curFreezeDuration, target.Health.SumDamageTaken + _damageToExit, _dad.gameObject, _skill.name);
			//Explode();
			GetComponent<Collider>().enabled = false;
		}
		else
		{
			_skill.ApplyDamage(_damage,target.gameObject);
			//damageable.TryTakeDamage(ref _damage, _skill);
			Explode();
		}
	}

    private IEnumerator CrutchDelay(Character target, float duration)
    {
        //yield return new WaitForSecondsRealtime(0.1f);
        yield return null;
		ApplyStateWithFrostEnergyBonus(target, States.Frozen, duration);
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

	private void ApplyStateWithFrostEnergyBonus(Character target, States state, float duration)
	{
		bool hasFrostEnergy = target.CharacterState.CheckForState(States.FrostEnergy);

		int currentStacks = target.CharacterState.CheckStateStacks(state);
		int stacksAfterApply = currentStacks + 1;

		float bonusPerStack = 0f;

		switch (state)
		{
			case States.Cooling:
				bonusPerStack = FrostEnergyCoolingBonusPerStack;
				break;

			case States.Frosting:
				bonusPerStack = FrostEnergyFrostingBonusPerStack;
				break;

			case States.Frozen:
				bonusPerStack = FrostEnergyFrozenBonusPerStack;
				break;

			default:
				bonusPerStack = 0f;
				break;
		}

		float bonusDamage = 0f;

		if (hasFrostEnergy && bonusPerStack > 0f)
		{
			bonusDamage = stacksAfterApply * bonusPerStack;

			Damage bonus = new Damage
			{
				Value = bonusDamage,
				Type = DamageType.Magical
			};

			_skill.ApplyDamage(_damage,target.gameObject);
			//target.Health.TryTakeDamage(ref bonus, _skill);
			TargetRpcDamageMake(bonusDamage);
		}

		target.CharacterState.AddState(state, duration,	target.Health.SumDamageTaken + _damageToExit, _dad.gameObject, _skill.name);
	}

	public void Talent(bool value, bool frozenState, bool lastHit)
	{
		_boostDmg = value;
		if(lastHit)
		{
			if (frozenState) _damageToExit = 60;
			else _damageToExit = 30;
		}
		else
		{
			_damageToExit = 1;
		}
	}

	private void Reflect(Character reflector)
	{
		_isReflected = true;

		Character oldOwner = _dad;
		_dad = reflector;

		if (oldOwner == null) return;

		_startPos = transform.position;

		if (_rb != null)
		{
			_rb.linearVelocity = Vector3.zero;
			_rb.angularVelocity = Vector3.zero;
		}

		Vector3 direction = (oldOwner.transform.position - transform.position).normalized;

		_rb.AddForce(direction * _force, ForceMode.Impulse);
	}
}
