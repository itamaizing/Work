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
	[SerializeField] private float _abilityCooldown = 1f;
	[SerializeField] private PlayerLinks _dad;
	private int _hitInARow = 0;
	private float _multiplySpeed = .05f;
	private HealthPlayer _target;
	private float _timer = 1f;
	private float _baseTimer = 1f;
	private bool _isInTheRow = false;


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
		Collider2D[] enemyDetected = Physics2D.OverlapCircleAll(transform.position, Radius);

		foreach (Collider2D col in enemyDetected)
		{
			if (col.gameObject == _dad)
				continue;
			if(TryGetComponent<PlayerLinks>(out var enemy))
			{
				Debug.Log("Enemy detected: " + col.gameObject.name);
				Hit(enemy.HealthPlayer);
				break;
			}			
		}
	}

	private void Hit(HealthPlayer enemy)
	{
		if(_target == enemy && _dad.Stamina.Use(5))
		{
			_hitInARow++;
			_timer = _baseTimer;
			enemy.TakeDamage(_damage + Random.Range(0, 2), DamageType.Physical);
			if (_hitInARow >= 6)
			{
				LastHit();
			}
		}
		else
		{
			_target = enemy;
			_isInTheRow = true;
			_timer = _baseTimer;
			_hitInARow = 0;
			_multiplySpeed = .05f;
			enemy.TakeDamage(_damage + Random.Range(0, 2), DamageType.Physical);
		}
	}
	private void LastHit()
	{
		if (_dad.Stamina.Use(10))
		{
			_target.TakeDamage(_damage * .5f, DamageType.Physical);
			//отбрасывание и стан
			
		}
		_dad.Stamina.Add(_dad.Stamina.MaxValue*0.4f);
		//regen 40 energy
		_hitInARow = 0;
		_target = null;
		_isInTheRow= false;
		_timer = _baseTimer;
	}

	private void Timer()
	{
		if (_isInTheRow)
		{
			_timer -= Time.deltaTime;
			if (_timer <= 0)
			{
				_timer = _baseTimer;
				_isInTheRow = false;
			}
		}
	}
}
