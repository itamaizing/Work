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

		_spriteRenderer.DOFade(0, 1);
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
		if (collision.gameObject == _dad.gameObject) return;

		if (!collision.TryGetComponent<IDamageable>(out var damageable))
			return;

		if (collision.TryGetComponent<Character>(out var target) && target != _dad)
		{
			float finalDamage = _curDamage;

			if (_boostDmg && target.CharacterState.CheckForState(States.Frozen))
			{
				finalDamage *= 1.4f;
			}

			_damage.Value = finalDamage;

			TargetRpcDamgeMake(finalDamage);
			target.Health.TryTakeDamage(ref _damage, _skill);

			target.CharacterState.AddState(States.Frozen, _curFreezeDuration, target.Health.SumDamageTaken + _damageToExit, _dad.gameObject, _skill.name);
			GetComponent<Collider>().enabled = false;
		}
		else
		{
			damageable.TryTakeDamage(ref _damage, _skill);
			Explode();
		}
	}

	private IEnumerator CrutchDelay(Character target, float duration)
	{
		//yield return new WaitForSecondsRealtime(0.1f);
		yield return null;
		target.CharacterState.AddState(States.Frozen, duration, target.Health.SumDamageTaken + _damageToExit, _dad.gameObject, _skill.name);
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
}
