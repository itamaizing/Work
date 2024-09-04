using DG.Tweening;
using UnityEngine;

public class PhysicalAttack : AutoAttackSkill
{
	[SerializeField] private float _damage = 8f;
	[SerializeField] private Character _character;
	[SerializeField] private SeriesOfStrikes _combo;

	private Character _curTarget;
	private Vector2 _jumpPos;
	private bool _talentActive = false;
	public Character Target2 => _curTarget;

	protected override void CastAction()
	{
		Hit(Target);
	}
	private void Hit(Character enemy)
	{
		if (_curTarget == enemy && _character.Stamina.TryUse(5))
		{
			Buff.AttackSpeed.ReductionPercentage(1 + _combo.GetMultipliedSpeed() / 100);


			float curDamage = _damage + Random.Range(0, 2);
			if(_combo.MakeHit(enemy, AbilityForm.Physical, 0, curDamage))
			{
				LastHit();
			}
			Buff.AttackSpeed.IncreasePercentage(1 + _combo.GetMultipliedSpeed()/100); // ?

			Damage damage = new Damage
			{
				Value = curDamage,
				Type = DamageType.Physical,
				Range = AttackRangeType.MeleeAttack,
			};
			CmdApplyDamage(damage, enemy.gameObject);

			//enemy.Health.TryTakeDamage(ref damage, this);
			//ApplyDamage(enemy.Health, curDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
			Energy energy = (Energy)_character.Stamina;
			if(enemy.CharacterState.CheckForState(States.Frozen))
			{
				curDamage *= 1.4f;
			}
			energy.SumDamageMake(curDamage);
		}
		else
		{
			Buff.AttackSpeed.ReductionPercentage(1 + _combo.GetMultipliedSpeed() / 100);
			Debug.Log("lose streak to another enemy");
			_curTarget = enemy;

			//AttackSpeed *= (1 - _combo.GetMultipliedSpeed()); // error

			float curDamage = _damage + Random.Range(0, 2);
			Energy energy = (Energy)_character.Stamina;
			energy.SumDamageMake(curDamage);

			_combo.MakeHit(enemy, AbilityForm.Physical, 0, curDamage);

			Damage damage = new Damage
			{
				Value = curDamage,
				Type = DamageType.Physical,
				Range = AttackRangeType.MeleeAttack,
			};
			CmdApplyDamage(damage, enemy.gameObject);
			//ApplyDamage(enemy.Health, curDamage, DamageType.Physical, AttackRangeType.MeleeAttack);

			Buff.AttackSpeed.IncreasePercentage(1 + _combo.GetMultipliedSpeed()/100); // ?
		}

		if (Random.Range(0, 100) <2 && _talentActive)
		{
			_character.RuneComponent.Add(1);
		}
	}
	private void LastHit()
	{
		if (_character.Stamina.TryUse(10))
		{
			Damage damage = new Damage
			{
				Value = _damage * 0.5f,
				Type = DamageType.Physical,
				Range = AttackRangeType.MeleeAttack,
			};
			CmdApplyDamage(damage, _curTarget.gameObject);
			//_curTarget.Health.TryTakeDamage(_damage * .5f, DamageType.Physical, AttackRangeType.MeleeAttack);
			float curDamage = _damage * .5f;
			Energy energy = (Energy)_character.Stamina;
			energy.SumDamageMake(curDamage);
			_curTarget.CharacterState.CmdAddState(States.Stun, 1.5f, 0, _character.gameObject, name);
			PushBackEnemy(_curTarget);
			//отбрасывание 			
		}
		_character.Stamina.Add(_character.Stamina.MaxValue*0.4f);
		_curTarget = null;
	}


	private void PushBackEnemy(Character enemy)
	{
		/*Debug.Log("Push");
		Vector2 pushPos = (_player.Rb.position - enemy.Rb.position).normalized;
		Vector2 endPos = -pushPos * 2;
		//enemy.PlayerMove.CanMove = false;
		//Debug.DrawLine(enemy.Rb.position, enemy.Rb.position + endPos * 10, Color.red, Mathf.Infinity);
		if (CheckObstacleBetween(enemy.Rigidbody2D.position, endPos))
		{
			enemy.Rigidbody2D.DOMove(_jumpPos, 1).SetEase(Ease.Linear);
		}
		else
		{
			enemy.Rb.DOMove(enemy.Rb.position + endPos, 1).SetEase(Ease.Linear);
		}*/
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

	public void SetTalentActive(bool active)
	{
		_talentActive = active;
	}
}
