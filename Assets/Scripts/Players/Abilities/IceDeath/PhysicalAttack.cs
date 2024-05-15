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
	[SerializeField] private GameObject _dad;
	private int _hitInARow = 0;
	private bool _isInTheRow = false;
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
			Debug.Log("Enemy detected: " + col.gameObject.name);
		}
	}

	/*private void LastHit()
	{
		//if player have 5 energy
		{
			_targetHealth.TakeDamage(_damageValue * .5f, DamageType, AttackRangeType);
			//отбрасывание и стан
			
		}
		//regen 40 energy
		_hitInARow = 0;
		_isInTheRow = false;
	}*/
}
