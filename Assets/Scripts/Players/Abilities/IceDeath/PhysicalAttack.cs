using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class PhysicalAttack : Skill
{
	[SerializeField] private SeriesOfStrikes _combo;
	[SerializeField] private AudioClip[] _hits;

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

	private bool _nextHitAppliesFrozen;
	private float _nextHitFrozenDuration;

	#region Const
	private const int EnergyPerAttack = 5;
	private const int HitVariationMax = 2;
	private const float TalentProcChance = 2f;
	private const float RollingStunTimeMultiplier = 0.7f;
	private const float DefaultMultiplier = 1f;
	private const float LastHitDamageMultiplier = 0.5f;
	private const float RandomAttack = 0.5f;
	private const float LastHitStunTime = 1.5f;
	private const float RadiusSearchTarget = 0.5f;
	private const float FrostEnergyFreezeChance = 60f;
	private static readonly Vector2 ObstacleCheckSize = new Vector2(1f, 1f);
	private static readonly int RightKickTrigger = Animator.StringToHash("RightKick");
	private static readonly int LeftKickTrigger = Animator.StringToHash("LeftKick");
	#endregion

	protected override int AnimTriggerCastDelay => 0;
	protected override int AnimTriggerCast => _animTriggerToUse = UnityEngine.Random.value > RandomAttack ? RightKickTrigger : LeftKickTrigger;
	protected override bool IsCanCast => Targeting.GetTarget()?.Character != null && Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius && Targeting.NoObstacles(Targeting.GetTarget().Character.transform.position, transform.position, _obstacle);

	private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

	private void OnEnable()
	{
		OnSkillCanceled += HandleSkillCanceled;
	}

	private void OnDisable()
	{
		OnSkillCanceled -= HandleSkillCanceled;
	}

	private void Start()
	{
		_audioSource = GetComponent<AudioSource>();
		_animator = GetComponent<Animator>();

        //_energy = (Energy)Hero.Resources[ResourceType.Energy];
        //_rune = (RuneComponent)Hero.Resources[ResourceType.Rune];

    }

	private void HandleSkillCanceled()
	{
		Targeting.ClearTarget();
		Targeting.ClearTempTarget();
		_hero.Move.StopLookAt();
		AnimCastEnded();
	}

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		TargetInfo targetInfo = new TargetInfo();

		if (Targeting.GetTempTarget()?.Character != null)
		{
			_hero.Move.LookAtTransform(Targeting.GetTarget()?.Character.transform);
			targetInfo.AddTarget(Targeting.GetTarget()?.Character);
			targetInfo.Points.Add(Targeting.GetTarget().Character.transform.position);
			callbackDataSaved?.Invoke(targetInfo);
			yield break;
		}

        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
			{
				Targeting.FindTempTarget(Targeting.GetMousePoint(), RadiusSearchTarget);

				if (Targeting.GetTempTarget()?.Character != null)
				{
					if (IsAllyTarget(Targeting.GetTempTarget()?.Character) || Targeting.GetTempTarget()?.Character == Hero)
					{
						Targeting.ClearTempTarget();						
					}
					else
					{
                        Targeting.GetTempTarget().Character.SelectedCircle.IsActive = true;
						_hero.Move.LookAtTransform(Targeting.GetTempTarget().Character.transform);
                        break;
					}
				}
			}
			yield return null;
		}

		Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        Targeting.ClearTempTarget();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
		targetInfo.Points.Add(Targeting.GetTarget().Character.transform.position);
		callbackDataSaved?.Invoke(targetInfo);
	}

	protected override IEnumerator CastJob()
	{
		if (Targeting.GetTarget()?.Character == null || _animator == null) yield break;
		yield break;
	}

	public void PhysicalAttackCast()
	{
		if (Targeting.GetTarget()?.Character == null ||
			Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) > AreaInfo.Radius ||
			!Targeting.NoObstacles(Targeting.GetTarget().Character.transform.position, transform.position, _obstacle))
		{
			TryCancel();
			return;
		}

		AnimStartCastCoroutine();
	}

	public void PhysicalAttackEnded()
	{
		AnimCastEnded();
	}

	public void ApplyAttackDamage()
	{
		if (Targeting.GetTarget()?.Character == null) return;

		if (_seriesPhysicalTalent) Hit(Targeting.GetTarget()?.Character);
		else SingleHit(Targeting.GetTarget()?.Character);

		if (!_hero.Abilities.SkillQueue.Skills.Contains(this))
		{
			Targeting.ClearTarget();
		}
		CmdPlayShotSound();
	}

	private void Hit(Character enemy)
	{
		if (_energy == null)
			_energy = (Energy)Hero.Resources[ResourceType.Energy];
		if (_rune == null)
			_rune = (RuneComponent)Hero.Resources[ResourceType.Rune];
		if (_curTarget == enemy && _energy.CurrentValue >= EnergyPerAttack)
		{
			float curDamage = _damageValue + UnityEngine.Random.Range(0, HitVariationMax);
			_combo.GetMultipliedSpeed();
			_multiplier = DefaultMultiplier + _combo.LastKnownSpeedMultiplier / 100;
			Debug.Log($"_multiplier: {_multiplier}");

			if (_energy.CurrentValue >= EnergyPerAttack)
			{
				if (_combo.MakeHit(enemy, Info.AbilityForm, 0, EnergyPerAttack, curDamage, _multiplier))
				{
					Debug.Log("Last hit");
					LastHit();
				}
			}

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
			CmdTryApplyCooling(enemy.gameObject);
			TryApplyNextHitFrozen(enemy);

			if (_rollingPhysTalent)
			{
				CmdState(_curTarget.gameObject, RollingStunTimeMultiplier * _stunCount);
			}
		}
		else
		{
			Buff.AttackSpeed.IncreasePercentage(_multiplier);
			_multiplier = 1;
			_curTarget = enemy;

			float curDamage = _damageValue + UnityEngine.Random.Range(0, HitVariationMax);
			_energy.SumDamageMake(curDamage);
			Debug.Log(_rune, _rune.gameObject);
			_rune.SumDamageMake(curDamage);

			_combo.MakeHit(enemy, Info.AbilityForm, 0, 0, curDamage, _multiplier);

			if (_energy.CurrentValue >= 5)
			{
				_energy.CmdUse(5);
				_multiplier = DefaultMultiplier + _combo.GetMultipliedSpeed() / 100;
				Buff.AttackSpeed.ReductionPercentage(_multiplier);
			}
			Damage damage = new Damage
			{
				Value = curDamage,
				Type = DamageType.Physical,
			};
			CmdApplyDamage(damage, enemy.gameObject);
			CmdTryApplyCooling(enemy.gameObject);
			TryApplyNextHitFrozen(enemy);
		}

		if (UnityEngine.Random.Range(0, 100) < TalentProcChance && _talentActive)
		{
			_rune.CmdAdd(DefaultMultiplier);
		}
	}

	private void LastHit()
	{
		if (_energy.CurrentValue >= 10)
		{
			Damage damage = new Damage
			{
				Value = _damageValue * LastHitDamageMultiplier,
				Type = DamageType.Physical,
			};
			CmdApplyDamage(damage, _curTarget.gameObject);
			float curDamage = _damageValue * LastHitDamageMultiplier;
			_energy.SumDamageMake(curDamage);
			_rune.SumDamageMake(curDamage);
			CmdState(_curTarget.gameObject, LastHitStunTime);
			PushBackEnemy(_curTarget); 			
		}
		_curTarget = null;
	}

	private void SingleHit(Character enemy)
	{
		Debug.Log("Single hit");
		float curDamage = _damageValue + UnityEngine.Random.Range(0, HitVariationMax);

		Damage damage = new Damage
		{
			Value = curDamage,
			Type = DamageType.Physical,
		};
		_combo.MakeHit(enemy, Info.AbilityForm, 0, EnergyPerAttack, curDamage, _multiplier);
		CmdApplyDamage(damage, enemy.gameObject);
		TryApplyNextHitFrozen(enemy);
	}

	[Command]
	private void CmdState(GameObject enemy, float time)
	{
		Character enemyChar = enemy.GetComponent<Character>();
		enemyChar.CharacterState.AddState(States.Stun, time, 0, Hero.gameObject, name);
	}

	private void PushBackEnemy(Character enemy)
	{
		Vector3 lookDir = (Targeting.GetTarget().Character.transform.position - Hero.transform.position).normalized;
		Vector3 jumpPos = lookDir * DefaultMultiplier + Targeting.GetTarget().Character.transform.position;
		if (!CheckObstacleBetween(Hero.transform.position, jumpPos))
		{
			CmdPush(Targeting.GetTarget()?.Character.gameObject, jumpPos);
		}
	}

	[Command]
	private void CmdPush(GameObject gameObject, Vector2 force)
	{
		MoveComponent tempTargetMove = gameObject.GetComponent<MoveComponent>();
	}

	[Command]
	private void CmdPlayShotSound()
	{
		RpcPlayShotSound();
	}

	[ClientRpc]
	private void RpcPlayShotSound()
	{
		if (_audioSource != null && _hits != null)
		{
			int index = UnityEngine.Random.Range(0, _hits.Length);
			_audioSource.PlayOneShot(_hits[index]);
		}
	}

	private bool CheckObstacleBetween(Vector3 start, Vector3 end)
	{
		Vector2 direction = (end - start).normalized;
		float distance = Vector3.Distance(start, end);

		RaycastHit2D[] hits =
			Physics2D.BoxCastAll(start, ObstacleCheckSize, 0f, direction, distance, _obstacle);

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
		Hero.Move.SetCanMove(false);
		_animator.applyRootMotion = true;
	}

	public void SetNextHitFrozen(float duration)
	{
		_nextHitAppliesFrozen = true;
		_nextHitFrozenDuration = duration;
	}

	private void TryApplyNextHitFrozen(Character enemy)
	{
		if (!_nextHitAppliesFrozen || enemy == null) return;

		enemy.CharacterState.AddState(States.Frozen, _nextHitFrozenDuration, 0f, Hero.gameObject, Name);

		_nextHitAppliesFrozen = false;
		_nextHitFrozenDuration = 0f;
	}

	[Command]
	private void CmdTryApplyCooling(GameObject enemyObj)
	{
		if (enemyObj == null) return;

		var enemy = enemyObj.GetComponent<Character>();
		if (enemy == null) return;

		if (enemy.CharacterState.CheckForState(States.FrostEnergy))
		{
			if (UnityEngine.Random.Range(0f, 100f) <= FrostEnergyFreezeChance) enemy.CharacterState.AddState(States.Cooling, 12f, 0f, Hero.gameObject, Name);
		}
	}

	public void ApplyRootFalse()
	{
		_animator.applyRootMotion = false;
	}

	public override void LoadTargetData(TargetInfo targetInfo)
	{
		if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
	}

    protected override void ClearData()
    {
		Targeting.ClearTarget();
		Targeting.ClearTempTarget();
		_hero.Move.StopLookAt();
	}
}

