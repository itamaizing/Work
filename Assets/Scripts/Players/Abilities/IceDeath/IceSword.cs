using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class IceSword : TargetOrAreaAbility
{
	[SerializeField] private float _damage = 15f;
	//[SerializeField] private GameObject _basePlayer;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private float _raduis;
	[SerializeField] private float _cooldownTime;
	private float _cooldownTimer = 1.4f;
	private int _hitInTheRow = 0;
	private bool _canUse = true;
	private Character _oldtarget;

	private void Update()
	{
		if (_canUse) return;
		Timer();
	}
	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	/*protected override void Cast()
	{
		if(!_canUse) return;

		/*PayCost();
		Collider2D[] colliders = Physics2D.OverlapCircleAll(gameObject.transform.position, _raduis);
		Debug.Log("try hit");
		foreach (Collider2D collider in colliders)
		{
			if (collider.TryGetComponent<Character>(out var enemy) && enemy != _playerLinks)
			{
				_combo.MakeHit(enemy, AbilityForm.Magic, 0);
				/*if (_target == enemy)
				{
					_cooldownTimer = _cooldownTime;
					_canUse = false;
					_hitInTheRow++;
					//_physicalAttack.HitFromSword(enemy);
					Debug.Log("hit from sword in a row");
				}
				else
				{
					_cooldownTimer = _cooldownTime;
					//_physicalAttack.LoseStreak();
					_hitInTheRow = 1;
					_canUse = false;
					_target = enemy;
					Debug.Log("first hit from sword");
				}
			}
		}
		if (_target != null)
		{
			ApplyDamage(Target.Health, _damage + Random.Range(0, 10), DamageType.Physical, AttackRangeType.MeleeAttack);
			//_target.Health.TryTakeDamage(_damage + Random.Range(0, 10), DamageType.Physical, AttackRangeType.MeleeAttack);
		}

		if( _hitInTheRow > 2 ) 
		{
			_deathSpiral.AddCharge();
			_hitInTheRow = 0;
		}
	}*/

	protected override void CastAction()
	{
		Debug.Log("SHOT SWORD");
		if (Target != null)
		{
			_seriesOfStrikes.MakeHit(Target, AbilityForm.Magic, 1);
			ApplyDamage(Target.Health, _damage, DamageType.Physical, AttackRangeType.MeleeAttack);
			//_target.Health.TryTakeDamage(_damage + Random.Range(0, 10), DamageType.Physical, AttackRangeType.MeleeAttack);
			if (_oldtarget == null || _oldtarget == Target)
			{
				_oldtarget = Target;
				_hitInTheRow++;
			}
			else
			{
				LoseStreak();
			}
		}
		if (_hitInTheRow > 2)
		{
			_deathSpiral.AddCharge();
			_hitInTheRow = 0;
		}
	}
	private void LoseStreak()
	{
		_hitInTheRow = 0;
		_target = null;
	}
	private void Timer()
	{
		Debug.Log("start timer");
		_cooldownTimer -= Time.deltaTime;
		if (_cooldownTimer <= 0)
		{
			_canUse = true;
			_cooldownTimer = _cooldownTime;
			//_physicalAttack.LoseStreak();
			//_IshitInTheRow = false;
			//_hitInTheRow = 0;
			//_target = null;
		}

	}
	/*protected override void PayCost()
	{
		if (Mana.Value >= _manaCost && _isReady)
		{
			Mana.Use(_manaCost);
		}
		else
		{
			TryCancel();
			return;
		}
		_isReady = false;
		_cooldownJob = StartCoroutine(CooldownCoroutine());
	}*/
}
