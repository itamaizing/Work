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

public class PhysicalAttack : Ability
{
	[SerializeField] private float _damage = 8f;
	[SerializeField] private PlayerLinks _dad;
	[SerializeField] private float _abilityCooldown = 1.4f; //cooldown between shots
	[SerializeField] private LayerMask _obstacleLayerMask;
	private float _cooldownTimer = 1.4f;
	[SerializeField] private int _hitInARow = 0;
	private float _multiplySpeed = .05f;
	private bool _isInTheRow = false;
	private float _baseTimer = 2f; //time and timer between losing streak
	[SerializeField] private float _timer = 2f;
	private bool _isReadyToShot = true;
	private PlayerLinks _target;
	private Vector2 _jumpPos;

	public PlayerLinks Target => _target;
	public int HitInTheRow => _hitInARow;

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
				//Debug.Log("Enemy detected: " + enemy.gameObject.name);
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
			//Debug.Log("hit " + _hitInARow);
			_hitInARow++;
			_multiplySpeed*=2;
			_timer = _baseTimer;
			_isInTheRow = true;

			enemy.HealthPlayer.TakeDamage(_damage + Random.Range(0, 2), DamageType.Physical);
			if (_hitInARow >= 6)
			{
				//Debug.Log("Lasthit");
				LastHit();
			}
		}
		else
		{
			//Debug.Log("lose streak to another enemy");
			_target = enemy;
			_hitInARow = 0;
			_multiplySpeed = .05f;
			_timer = _baseTimer;
			_isInTheRow = true;
			
			enemy.HealthPlayer.TakeDamage(_damage + Random.Range(0, 2), DamageType.Physical);
		}
	}
	private void LastHit()
	{
		if (_dad.Stamina.Use(10))
		{
			_target.HealthPlayer.TakeDamage(_damage * .5f, DamageType.Physical);
			_target.CharacterState.AddState(new StunnedState(), 1.5f, 0, States.Stun);
			PushBackEnemy(_target);
			//отбрасывание 			
		}
		_dad.Stamina.Add(_dad.Stamina.MaxValue*0.4f);
		_hitInARow = 0;
		_target = null;
		_isInTheRow= false;
		_multiplySpeed = 0.05f;
		_timer = _baseTimer;
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
			_cooldownTimer = _abilityCooldown * (1 - _multiplySpeed);
		}
		if (_isInTheRow)
		{
			_timer -= Time.deltaTime;
			if (_timer <= 0)
			{
				_target = null;
				_multiplySpeed = 0.05f;
				Debug.Log("lose streak");
				_timer = _baseTimer;
				_isInTheRow = false;
				_hitInARow = 0;
			}
		}
	}

	private void PushBackEnemy(PlayerLinks enemy)
	{
		Debug.Log("Push");
		Vector2 pushPos = (_dad.Rb.position - enemy.Rb.position).normalized;
		Vector2 endPos = -pushPos * 2;
		//enemy.PlayerMove.CanMove = false;
		//Debug.DrawLine(enemy.Rb.position, enemy.Rb.position + endPos * 10, Color.red, Mathf.Infinity);
		if (CheckObstacleBetween(enemy.Rb.position, endPos))
		{
			enemy.Rb.DOMove(_jumpPos, 1).SetEase(Ease.Linear);
		}
		else
		{
			enemy.Rb.DOMove(enemy.Rb.position + endPos, 1).SetEase(Ease.Linear);
		}
	}

	private bool CheckObstacleBetween(Vector3 start, Vector3 end)
	{
		//Проверка на наличие препятствия
		Vector2 direction = (end - start).normalized;
		float distance = Vector2.Distance(start, end);

		RaycastHit2D[] hits =
			Physics2D.BoxCastAll(start, new Vector2(1f, 1f), 0f, direction, distance, _obstacleLayerMask);

		foreach (RaycastHit2D hit in hits)
		{
			Debug.Log(hit.collider.gameObject.name);
			_jumpPos = hits[0].point - direction;
			return true;
		}

		return false;
	}

	public void HitFromSword(int hitInTheRow, float multiplySpeed)
	{
		_hitInARow = hitInTheRow;
		_multiplySpeed = multiplySpeed;
		_timer = _baseTimer;
		_isInTheRow = true;
	}

	public void HitFromSword(PlayerLinks enemy)
	{
		Debug.Log("hit from sword");
		_target = enemy;
		_hitInARow++;
		_multiplySpeed *= 2;
		_timer = _baseTimer;
		_isInTheRow = true;
	}

	public void LoseStreak()
	{
		_target = null;
		_multiplySpeed = 0.05f;
		Debug.Log("lose streak");
		_timer = _baseTimer;
		_isInTheRow = false;
		_hitInARow = 0;
	}
}
