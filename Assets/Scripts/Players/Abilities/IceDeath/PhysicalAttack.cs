using DG.Tweening;
using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class PhysicalAttack : Skill
{
	//[SerializeField] private float _damage = 8f;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _combo;
	[SerializeField] private AudioClip[] Hits;

	private AudioSource _audioSource;
	private Character _curTarget;
	private Vector2 _jumpPos;
	private Energy _energy;
	private RuneComponent _rune;
	private float _multiplier = 1;
	private bool _talentActive = false;
	private bool _rollingPhysTalent = false;
	private bool _seriesPhysicalTalent;
	private float _stunCount = 0;
	private int _animTriggerToUse = 0;
	private bool _isRightKick = true;
	private Animator _animator;

	protected Character _target;

	private static readonly int RightKickTrigger = Animator.StringToHash("RightKick");
	private static readonly int LeftKickTrigger = Animator.StringToHash("LeftKick");

	protected override int AnimTriggerCastDelay => 0;

	protected override int AnimTriggerCast => _animTriggerToUse = UnityEngine.Random.value > 0.5f ? RightKickTrigger : LeftKickTrigger;

	protected override bool IsCanCast => _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius && NoObstacles(_target.transform.position, transform.position, _obstacle);
	private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

	private void Start()
	{
		_audioSource = GetComponent<AudioSource>();
		_animator = GetComponent<Animator>();

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

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		TargetInfo targetInfo = new TargetInfo();

		if (_target != null)
		{
			_hero.Move.LookAtTransform(_target.transform);
			targetInfo.Targets.Add(_target);
			targetInfo.Points.Add(_target.transform.position);
			callbackDataSaved?.Invoke(targetInfo);
			yield break;
		}

		while (_target == null)
		{
			if (GetMouseButton)
			{
				_target = GetTarget().character;

				if (_target != null)
				{
					if (IsAllyTarget(_target) || _target == Hero)
					{
						_target = null;						
					}
					else
					{
						_target.SelectedCircle.IsActive = true;
						_hero.Move.LookAtTransform(_target.transform);
						break;
					}
				}
			}
			yield return null;
		}

		targetInfo.Targets.Add(_target);
		targetInfo.Points.Add(_target.transform.position);
		callbackDataSaved?.Invoke(targetInfo);
	}

	protected override IEnumerator CastJob()
	{
		if (_target == null || _animator == null) yield break;
		yield break;
	}

	public void PhysicalAttackCast()
	{
		AnimStartCastCoroutine();
	}

	public void PhysicalAttackEnded()
	{
		AnimCastEnded();
	}

	public void ApplyAttackDamage()
	{
		if (_target == null) return;

		if (_seriesPhysicalTalent) Hit(_target);
		else SingleHit(_target);

		if (!_hero.Abilities.SkillQueue.Skills.Contains(this))
		{
			_target = null;
		}
		CmdPlayShotSound();
	}

	private void Hit(Character enemy)
	{
		Debug.Log(_energy.CurrentValue + " Current value");
		if (_curTarget == enemy && _energy.CurrentValue >= 5)
		{
			//_energy.CmdUse(5);
			Buff.AttackSpeed.IncreasePercentage(_multiplier);
			Buff.CastSpeed.IncreasePercentage(_multiplier);
			float curDamage = _damageValue + UnityEngine.Random.Range(0, 2);

			if (_energy.CurrentValue >= 5)
			{
				if (_combo.MakeHit(enemy, AbilityForm.Physical, 0, 5, curDamage))
				{
					Debug.Log("Last hit");
					LastHit();
				}
			}
			_multiplier = 1 + _combo.GetMultipliedSpeed() / 100;
			Buff.AttackSpeed.ReductionPercentage(_multiplier);
			Buff.CastSpeed.IncreasePercentage(_multiplier);

			Damage damage = new Damage
			{
				Value = curDamage,
				Type = DamageType.Physical,
			};


			if (enemy.CharacterState.CheckForState(States.Frozen))
			{
				curDamage *= 1.4f;
			}
			_energy.SumDamageMake(curDamage);
			_rune.SumDamageMake(curDamage);
			_energy.CmdUse(5);
			CmdApplyDamage(damage, enemy.gameObject);

			if (_rollingPhysTalent)
			{
				CmdState(_curTarget.gameObject, 0.7f * _stunCount);
			}
		}
		else
		{
			Buff.AttackSpeed.IncreasePercentage(_multiplier);
			_multiplier = 1;
			Debug.Log("lose streak to another enemy or no energy");
			_curTarget = enemy;

			float curDamage = _damageValue + UnityEngine.Random.Range(0, 2);
			_energy.SumDamageMake(curDamage);
			_rune.SumDamageMake(curDamage);

			_combo.MakeHit(enemy, AbilityForm.Physical, 0, 0, curDamage);

			if (_energy.CurrentValue >= 5)
			{
				_energy.CmdUse(5);
				_multiplier = 1 + _combo.GetMultipliedSpeed() / 100;
				Buff.AttackSpeed.ReductionPercentage(_multiplier); // ?
			}
			Damage damage = new Damage
			{
				Value = curDamage,
				Type = DamageType.Physical,
			};
			CmdApplyDamage(damage, enemy.gameObject);
		}

		if (UnityEngine.Random.Range(0, 100) < 2 && _talentActive)
		{
			_rune.CmdAdd(1);
			//Debug.Log(_rune.CurrentValue + " REGEN Current value");
		}
	}

	private void LastHit()
	{
		if (_energy.CurrentValue >= 10)
		{
			Damage damage = new Damage
			{
				Value = _damageValue * 0.5f,
				Type = DamageType.Physical,
			};
			CmdApplyDamage(damage, _curTarget.gameObject);
			float curDamage = _damageValue * .5f;
			_energy.SumDamageMake(curDamage);
			_rune.SumDamageMake(curDamage);
			CmdState(_curTarget.gameObject, 1.5f);
			PushBackEnemy(_curTarget);
			//отбрасывание 			
		}
		//_energy.Add(_energy.MaxValue*0.4f);
		_curTarget = null;
	}

	private void SingleHit(Character enemy)
	{
		float curDamage = _damageValue + UnityEngine.Random.Range(0, 2);

		Damage damage = new Damage
		{
			Value = curDamage,
			Type = DamageType.Physical,
		};
		_combo.MakeHit(enemy, AbilityForm.Physical, 0, 5, curDamage);
		CmdApplyDamage(damage, enemy.gameObject);
	}

	[Command]
	private void CmdState(GameObject enemy, float time)
	{
		Character enemyChar = enemy.GetComponent<Character>();
		enemyChar.CharacterState.AddState(States.Stun, time, 0, _playerLinks.gameObject, name);
		Debug.Log("added state");
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

		//tempTargetMove.TargetRpcDoMove(force, 0.5f);
	}

	[Command]
	private void CmdPlayShotSound()
	{
		RpcPlayShotSound();
	}

	[ClientRpc]
	private void RpcPlayShotSound()
	{
		if (_audioSource != null && Hits != null)
		{
			int index = UnityEngine.Random.Range(0, Hits.Length);
			_audioSource.PlayOneShot(Hits[index]);
		}
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

	#region Talent

	public void SeriesPhysicalTalentActive(bool value)
	{
		_seriesPhysicalTalent = value;
	}

	public void SetTalentActive(bool active)
	{
		_talentActive = active;
	}

	public void TalentRollingPhys(bool value, float count)
	{
		_rollingPhysTalent = value;
		_stunCount = count;
	}

	#endregion

	public void ApplyRootTrue()
	{
		Hero.Move.CanMove = false;
		_animator.applyRootMotion = true;
	}

	public void ApplyRootFalse()
	{
		_animator.applyRootMotion = false;
	}

	public override void LoadTargetData(TargetInfo targetInfo)
	{
		if (targetInfo.Targets.Count > 0) _target = (Character)targetInfo.Targets[0];
	}

    protected override void ClearData()
    {
		_target = null;
		_hero.Move.StopLookAt();
	}
}