using System.Collections;
using System.Collections.Generic;
using GlobalEvents;
using Players.Abilities.Genjalf;
using Players.Abilities.Genjalf.Shield_Ability;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class PhysicalAttack : Ability
{
	[SerializeField] private float _damage = 8f;
	[SerializeField] private float _abilityCooldown = 1.4f;
	[SerializeField] private PlayerLinks _dad;
	[SerializeField] private float _cooldownTimer = 1.4f;
	[SerializeField] private int _hitInARow = 0;
	[SerializeField] private float _multiplySpeed = .05f;
	[SerializeField] private float _timer = 2f;
	[SerializeField] private bool _isInTheRow = false;
	private float _baseTimer = 2f;
	private bool _isReadyToShot = true;
	private PlayerLinks _target;

	private void Update()
	{
		Timer();
	}
	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	protected override void Cast()
	{
		PayCost();
		CheckEnemy();
	}
	
	private void CheckEnemy()
	{
        if (!_isReadyToShot)
        {
			return;
        }
        Collider2D[] enemyDetected = Physics2D.OverlapCircleAll(transform.position, Radius);

		foreach (Collider2D col in enemyDetected)
		{
			if(col.TryGetComponent<PlayerLinks>(out var enemy))
			{
				if (enemy == _dad)
				{
					continue;
				}				
				Debug.Log("Enemy detected: " + enemy.gameObject.name);
				Hit(enemy);
				break;
			}			
		}
	}

	private void Hit(PlayerLinks enemy)
	{
		_isReadyToShot = false;
		if(_target == enemy && _dad.Stamina.Use(5))
		{
			Debug.Log("hit " + _hitInARow);
			_hitInARow++;
			_multiplySpeed*=2;
			_timer = _baseTimer;
			_isInTheRow = true;
			enemy.HealthPlayer.TakeDamage(_damage + Random.Range(0, 2), DamageType.Physical);
			if (_hitInARow >= 6)
			{
				Debug.Log("Lasthit");
				LastHit();
			}
		}
		else
		{
			Debug.Log("lose streak to another enemy");
			_target = enemy;
			_isInTheRow = true;
			_timer = _baseTimer;
			_hitInARow = 0;
			_multiplySpeed = .05f;
			
			enemy.HealthPlayer.TakeDamage(_damage + Random.Range(0, 2), DamageType.Physical);
		}
	}
	private void LastHit()
	{
		if (_dad.Stamina.Use(10))
		{
			_target.HealthPlayer.TakeDamage(_damage * .5f, DamageType.Physical);
			_target.CharacterState.AddState(new StunnedState(), 1.5f, 0, States.Stun);
			//отбрасывание 			
		}
		_dad.Stamina.Add(_dad.Stamina.MaxValue*0.4f);
		//regen 40 energy
		_hitInARow = 0;
		_target = null;
		_isInTheRow= false;
		_multiplySpeed = 0.05f;
		_timer = _baseTimer;
	}

	private void Timer()
	{
		if(_cooldownTimer > 0 && !_isReadyToShot) 
		{
			_cooldownTimer -= Time.deltaTime;
		}
		else
		{
			_isReadyToShot = true;
			_cooldownTimer = _abilityCooldown * (1 - _multiplySpeed);
		}
		if (_isInTheRow)
		{
			_timer -= Time.deltaTime;
			if (_timer <= 0)
			{
				_multiplySpeed = 0.05f;
				Debug.Log("lose streak");
				_timer = _baseTimer;
				_isInTheRow = false;
				_hitInARow = 0;
			}
		}
	}
}
