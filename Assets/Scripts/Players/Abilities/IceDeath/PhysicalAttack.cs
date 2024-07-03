using DG.Tweening;
using UnityEngine;

public class PhysicalAttack : AutoAttackAbility
{
	[SerializeField] private float _damage = 8f;
	[SerializeField] private Character _dad;
	[SerializeField] private SeriesOfStrikes _combo;
	[SerializeField] private float _abilityCooldown = 1.4f; //cooldown between shots

	private float _baseTimer = 2f; //time and timer between losing streak
	private float _timer = 2f;
	private Character _curTarget;
	private Vector2 _jumpPos;

	public Character Target2 => _curTarget;


	/*private void Update()
	{
		Timer();
	}*/
	protected override void Cancel() { }

	protected override void CastAction()
	{
		Hit(Target);
		//Target.Health.TakeDamage(_damage, DamageType.Physical);
	}
	private void Hit(Character enemy)
	{
		if (_curTarget == enemy && _dad.Stamina.Use(5))
		{
			_combo.MakeHit(enemy, AbilityForm.Physical, 5);

			//AttackSpeed *= (1 - _combo.GetMultipliedSpeed()); // Error
			Buff.AttackSpeed.IncreasePercentage(1 - _combo.GetMultipliedSpeed()); // ?

			float curDamage = _damage + Random.Range(0, 2);

			ApplyDamage(enemy.Health, curDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
			Energy energy = (Energy)_dad.Stamina;
			if(enemy.CharacterState.CheckForState(States.Frozen))
			{
				curDamage *= 1.4f;
			}
			energy.SumDamageMake(curDamage);
		}
		else
		{
			_combo.MakeHit(enemy, AbilityForm.Physical, 0);
			Debug.Log("lose streak to another enemy");
			_curTarget = enemy;

			//AttackSpeed *= (1 - _combo.GetMultipliedSpeed()); // error
			Buff.AttackSpeed.IncreasePercentage(1 - _combo.GetMultipliedSpeed()); // ?

			_timer = _baseTimer;
			float curDamage = _damage + Random.Range(0, 2);
			Energy energy = (Energy)_dad.Stamina;
			energy.SumDamageMake(curDamage);
			ApplyDamage(enemy.Health, curDamage, DamageType.Physical, AttackRangeType.MeleeAttack);


		}
	}
	/*private void LastHit()
	{
		if (_dad.Stamina.Use(10))
		{
			_curTarget.Health.TakeDamage(_damage * .5f, DamageType.Physical);
			float curDamage = _damage * .5f;
			Energy energy = (Energy)_dad.Stamina;
			energy.SumDamageMake(curDamage);
			_curTarget.CharacterState.AddState(new StunnedState(), 1.5f, 0, States.Stun);
			PushBackEnemy(_curTarget);
			//отбрасывание 			
		}
		_dad.Stamina.Add(_dad.Stamina.MaxValue*0.4f);
		//_hitInARow = 0;
		_curTarget = null;
		//_isInTheRow= false;
		//_multiplySpeed = 0.05f;
		//_attackSpeed *= (1 - _multiplySpeed);
		_timer = _baseTimer;
	}*/

	/*public void Timer()
	{
		/*if(_cooldownTimer > 0 && !_isReadyToShot) 
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
				_curTarget = null;
				_multiplySpeed = 0.05f;
				_attackSpeed *= (1 - _multiplySpeed);
				Debug.Log("lose streak");
				_timer = _baseTimer;
				_isInTheRow = false;
				_hitInARow = 0;
			}
		}
	}*/

	private void PushBackEnemy(Character enemy)
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
			Physics2D.BoxCastAll(start, new Vector2(1f, 1f), 0f, direction, distance, _obstacle);

		foreach (RaycastHit2D hit in hits)
		{
			Debug.Log(hit.collider.gameObject.name);
			_jumpPos = hits[0].point - direction;
			return true;
		}

		return false;
	}

	/*public void HitFromSword(int hitInTheRow, float multiplySpeed)
	{
		_hitInARow = hitInTheRow;
		_multiplySpeed = multiplySpeed;
		_timer = _baseTimer;
		_isInTheRow = true;
	}

	public void HitFromSword(Character enemy)
	{
		Debug.Log("hit from sword");
		_curTarget = enemy;
		_hitInARow++;
		_multiplySpeed *= 2;
		_timer = _baseTimer;
		_isInTheRow = true;
	}

	public void LoseStreak()
	{
		_curTarget = null;
		_multiplySpeed = 0.05f;
		Debug.Log("lose streak");
		_timer = _baseTimer;
		_isInTheRow = false;
		_hitInARow = 0;
	}*/
}
