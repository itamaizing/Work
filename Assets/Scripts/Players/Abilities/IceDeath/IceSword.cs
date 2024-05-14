using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceSword : Ability
{
	[SerializeField] private float _damage = 15f;
	//[SerializeField] private GameObject _basePlayer;
	[SerializeField] private PlayerLinks _playerLinks;
	[SerializeField] private DeathSpiral _deathSpiral;
	//private Vector2 _targetPosition;
	private HealthPlayer _target;
	private int _hitInARow = 0;

	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	protected override void Cast()
	{
		PayCost();
		Collider2D[] colliders = Physics2D.OverlapCircleAll(gameObject.transform.position, Radius);

		foreach (Collider2D collider in colliders)
		{
			if (collider.TryGetComponent<HealthPlayer>(out var enemy) && collider.gameObject != _playerLinks.gameObject)
			{
				Debug.Log(collider.name);
				//enemy.
				//check closest and then damage
				if (_target == null || _target == enemy)
				{
					_target = enemy;
					_hitInARow++;
				}
				else
				{
					_hitInARow = 0;
					_target = enemy;
				}
			}
		}
		_target.TakePhisicDamage(_damage + Random.Range(0, 10));

		if( _hitInARow > 2 ) 
		{
			_deathSpiral.AddCharge();
			_hitInARow = 0;
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
