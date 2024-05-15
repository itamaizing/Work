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
	[SerializeField] private float _damage = 1f;
	[SerializeField] private float _abilityCooldown = 1f;
	[SerializeField] private PlayerLinks _dad;
	private int _hitInARow = 0;
	private float _multiplySpeed = .05f;
	private HealthPlayer _target;

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
			enemy.TakeDamage(_damage, DamageType.Physical);
			if (_hitInARow >= 6)
			{
				LastHit();
			}
		}
		else
		{
			_target = enemy;
			_hitInARow = 0;
			_multiplySpeed = .05f;
			enemy.TakeDamage(_damage, DamageType.Physical);
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
	}

	private void Timer()
	{
		//if timer <0
		//hit in a row = 0;
		//_target = null;
	}
}
