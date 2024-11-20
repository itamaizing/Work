using DG.Tweening;
using Mirror;
using UnityEngine;

public class PhysicalAttack : AutoAttackSkill
{
	//[SerializeField] private float _damage = 8f;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _combo;

	private Character _curTarget;
	private Vector2 _jumpPos;
	private bool _talentActive = false;
	private Energy _energy;
	private RuneComponent _rune;
	private float _multiplier = 1;

    protected override int AnimTriggerCastDelay => 0;

	protected override int AnimTriggerAutoAttack => 0;
    private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
			if (_playerLinks.Resources[i].Type == ResourceType.Rune)
			{
				_rune = (RuneComponent)_playerLinks.Resources[i];
			}
		}
	}

	protected override void CastAction()
	{
		if(_target != null) 
		Hit(_target);
	}
	private void Hit(Character enemy)
	{
		//Debug.Log(AttackS)
		if (_curTarget == enemy && _energy.CurrentValue >= 5)
		{
			//_energy.CmdUse(5);
			Buff.AttackSpeed.IncreasePercentage(_multiplier);
			float curDamage = _damageValue + Random.Range(0, 2);
			
			if(_combo.MakeHit(enemy, AbilityForm.Physical, 0, curDamage))
			{
				Debug.Log("Last hit");
				LastHit();
			}
			_multiplier = 1 + _combo.GetMultipliedSpeed() / 100;
			Buff.AttackSpeed.ReductionPercentage(_multiplier); // ?

			Damage damage = new Damage
			{
				Value = curDamage,
				Type = DamageType.Physical,
			};
			CmdApplyDamage(damage, enemy.gameObject);

			if(enemy.CharacterState.CheckForState(States.Frozen))
			{
				curDamage *= 1.4f;
			}
			_energy.SumDamageMake(curDamage);
			_energy.CmdUse(5);
		}
		else
		{
			Buff.AttackSpeed.IncreasePercentage(_multiplier);
			
			Debug.Log("lose streak to another enemy");
			_curTarget = enemy;

			float curDamage = _damageValue + Random.Range(0, 2);
			_energy.SumDamageMake(curDamage);

			_combo.MakeHit(enemy, AbilityForm.Physical, 0, curDamage);

			Damage damage = new Damage
			{
				Value = curDamage,
				Type = DamageType.Physical,
			};
			CmdApplyDamage(damage, enemy.gameObject);
			_multiplier = 1 + _combo.GetMultipliedSpeed() / 100;
			Buff.AttackSpeed.ReductionPercentage(_multiplier); // ?
		}

		if (Random.Range(0, 100) <2 && _talentActive)
		{
			_rune.Add(1);
		}
	}
	private void LastHit()
	{
		if (_energy.CurrentValue >= 10)
		{
			//_energy.CmdUse(10);
			Damage damage = new Damage
			{
				Value = _damageValue * 0.5f,
				Type = DamageType.Physical,
			};
			CmdApplyDamage(damage, _curTarget.gameObject);
			//_curTarget.Health.TryTakeDamage(_damage * .5f, DamageType.Physical, AttackRangeType.MeleeAttack);
			float curDamage = _damageValue * .5f;
			_energy.SumDamageMake(curDamage);
			_curTarget.CharacterState.CmdAddState(States.Stun, 1.5f, 0, _playerLinks.gameObject, name);
			PushBackEnemy(_curTarget);
			//отбрасывание 			
		}
		_energy.Add(_energy.MaxValue*0.4f);
		_curTarget = null;
	}


	private void PushBackEnemy(Character enemy)
	{
		Vector3 lookDir = (_target.transform.position - _playerLinks.transform.position).normalized;
		Vector3 jumpPos = lookDir * 1 + _target.transform.position;
		if (!CheckObstacleBetween(_playerLinks.transform.position, jumpPos))
		{
			CmdPush(_target.gameObject, jumpPos);
			//прыгать до препятствия
		}
	}

	[Command]
	private void CmdPush(GameObject gameObject, Vector2 force)
	{
		MoveComponent tempTargetMove = gameObject.GetComponent<MoveComponent>();
		
		tempTargetMove.TargetRpcDoMove(force, 0.5f);
	}

	private bool CheckObstacleBetween(Vector3 start, Vector3 end)
	{
		//Проверка на наличие препятствия
		Vector2 direction = (end - start).normalized;
		float distance = Vector3.Distance(start, end);

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
