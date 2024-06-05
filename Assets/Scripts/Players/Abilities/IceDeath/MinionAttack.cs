using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GlobalEvents;
using Players.Abilities.Genjalf;
using Players.Abilities.Genjalf.Shield_Ability;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;

public class MinionAttack : Ability
{
	[SerializeField] private float _damage = 3f;
	[SerializeField] private Character _dad;
	[SerializeField] private float _abilityCooldown = 1.6f; //cooldown between shots
	[SerializeField] private LayerMask _obstacleLayerMask;
	private float _cooldownTimer = 1.6f;
	private bool _isReadyToShot = true;

	private void Update()
	{
		if (_isReadyToShot) return;
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
			if(col.TryGetComponent<Character>(out var enemy))
			{
				if (enemy == _dad)
				{
					continue;
				}				
				//Debug.Log("Enemy detected: " + enemy.gameObject.name);
				Hit(enemy);
				break;
			}			
		}
	}
	private void Hit(Character enemy)
	{
		_isReadyToShot = false;
		enemy.Health.TakeDamage(_damage + Random.Range(0, 1), DamageType.Physical);
	}

	public void Timer()
	{
		if(_cooldownTimer > 0 && !_isReadyToShot) 
		{
			_cooldownTimer -= Time.deltaTime;
		}
		else
		{
			_isReadyToShot = true;
			_cooldownTimer = _abilityCooldown;
		}
	}
}
