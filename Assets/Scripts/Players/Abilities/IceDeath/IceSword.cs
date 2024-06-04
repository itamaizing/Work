using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class IceSword : Ability
{
	[SerializeField] private float _damage = 15f;
	//[SerializeField] private GameObject _basePlayer;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private PhysicalAttack _physicalAttack;
	//private Vector2 _targetPosition;
	[SerializeField] private float _raduis;
	[SerializeField] private float _cooldowns;
	[SerializeField] private float _cooldownTimer = 1.4f;
	private int _hitInTheRow = 0;
	[SerializeField] private bool _canUse = true;
	private PlayerLinks _target;

	private void Update()
	{
		if (_canUse) return;
		Timer();
	}
	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	protected override void Cast()
	{
		if(!_canUse) return;

		PayCost();
		Collider2D[] colliders = Physics2D.OverlapCircleAll(gameObject.transform.position, _raduis);
		Debug.Log("try hit");
		foreach (Collider2D collider in colliders)
		{
			if (collider.TryGetComponent<PlayerLinks>(out var enemy) && enemy != _playerLinks)
			{
				if (_target == enemy || enemy == _physicalAttack.Target)
				{
					_cooldownTimer = _cooldowns;
					_canUse = false;
					_hitInTheRow++;
					_physicalAttack.HitFromSword(enemy);
					Debug.Log("hit from sword in a row");
				}
				else
				{
					_cooldownTimer = _cooldowns;
					_physicalAttack.LoseStreak();
					_hitInTheRow = 1;
					_canUse = false;
					_target = enemy;
					Debug.Log("first hit from sword");
				}
			}
		}
		if (_target != null)
		{
			_target.HealthPlayer.TakePhisicDamage(_damage + Random.Range(0, 10));
		}

		if( _hitInTheRow > 2 ) 
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
			_cooldownTimer = _cooldowns;
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
