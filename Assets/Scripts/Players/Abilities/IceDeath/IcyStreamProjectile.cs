using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class IcyStreamProjectile : Projectiles
{
	[HideInInspector] public FrostingFrozenTalant talant;

	private float _timer = 1.5f;
	private float _time = 0;
	private float _stepOfTimer = 0.3f;
	private float _damage = 1;
	private float _chanceOfFrosting = 0.05f;
	private float _durationOfDebuff = 2;
	private List<Character> _enemyList;
	//private Vector2 startPos;

	private void Awake()
	{
		Debug.Log("bullet");
		//startPos = transform.position;
		//_rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
		if (_energyDad >= 40)
		{
			_durationOfDebuff += 4;
		}
		else
		{
			_durationOfDebuff += _energyDad / 10;
		}
		_rb.DOMove(transform.up * _distance * GlobalVariable.cellSize, _timer).OnComplete(Explode);
	}

	private void Update()
	{
		Timer();
		_spriteRenderer.DOFade(0, _timer);
		/*if (Vector2.Distance(transform.position, startPos) > _distance * GlobalVariable.cellSize)
		{
			Explode();
		}*/
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (!_initialized || _dad == null) return;
		if (collision.gameObject == _dad.gameObject || collision.CompareTag("Ability"))
			return;

		if (collision.TryGetComponent<Character>(out var target))
		{
			_enemyList.Add(target);
			Energy energyLink = (Energy)_dad.Stamina;
			energyLink.SumDamageMake(_damage);
			target.Health.TryTakeDamage(_damage, DamageType.Magical, AttackRangeType.RangeAttack);
			target.CharacterState.CmdAddState(States.Cooling, _durationOfDebuff, 0);
		}
		//Explode();
	}
	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject == _dad.gameObject || collision.CompareTag("Ability"))
			return;

		if (collision.TryGetComponent<Character>(out var target))
		{
			_enemyList.Remove(target);
		}
	}
	private void Explode()
	{
		_enemyList.Clear();
		if (_hitEffect != null)
		{
			GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
			Destroy(hitEffect, 5f);
		}
		Destroy(gameObject);
	}

	private void Timer()
	{
		_time += Time.deltaTime;
		if(_time >= _stepOfTimer)
		{
			_time = 0;
			_distance++;
			_chanceOfFrosting *= 2;
			foreach(var enemy in _enemyList) 
			{
				enemy.Health.TryTakeDamage(_damage, DamageType.Magical, AttackRangeType.RangeAttack);

				enemy.CharacterState.CmdAddState(States.Cooling, _durationOfDebuff, 0);
				if (Random.Range(0, 1f) <= _chanceOfFrosting)
				{
					enemy.CharacterState.CmdAddState(States.Frosting, _durationOfDebuff, 0);
					if(talant != null)
					{
						if(talant.IsActive)
						{
							enemy.CharacterState.CmdAddState(States.Frozen, _durationOfDebuff, 0);
						}
					}
				}
			}
		}
	}
}
