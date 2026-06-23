using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class PhysicalAttack : Skill,IEnergyDamagable, IComboSeriesParticipatingSkill
{
	[SerializeField] private AudioClip[] _hits;

	private AudioSource _audioSource;
	private Character _curTarget;
	private Vector2 _jumpPos;
	private Energy _energy;
	private RuneComponent _rune;
	private float _multiplier = 1;
	private float _energyUsedSum;
	private int _animTriggerToUse = 0;
	private bool _isRightKick = true;
	private Animator _animator;
	
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

	private Character _castTarget;
	
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
		if (_castTarget == null) return;

		SingleHit(_castTarget);

		if (!_hero.Abilities.SkillQueue.Skills.Contains(this))
		{
			Targeting.ClearTarget();
		}
		CmdPlayShotSound();
	}


	private void SingleHit(Character enemy)
	{
		OnSeriesDamaged?.Invoke(enemy.gameObject,this);
		float curDamage = _damageValue + UnityEngine.Random.Range(0, HitVariationMax);

		Damage damage = new Damage
		{
			Value = curDamage * _currentDamageMultiplier,
			Type = DamageType.Physical,
		};
		CmdApplyDamage(damage, enemy.gameObject);
		_currentDamageMultiplier = 1;
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

	public void ApplyRootTrue()
	{
		Hero.Move.SetCanMove(false);
		_animator.applyRootMotion = true;
	}

	public override void LoadTargetData(TargetInfo targetInfo)
	{
		if (targetInfo.GetTargets().Count > 0)
		{
			Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
			_castTarget = Targeting.GetTarget()?.Character;
		}
	}

	protected override void ClearData()
	{
		_castTarget = null;
		Targeting.ClearTarget();
		Targeting.ClearTempTarget();
		_hero.Move.StopLookAt();
	}

	#region Series
	
	private const float KnockbackDistance = 1f;
	private const float KnockbackDuration = 0.35f;
	private float _bonusDamageMultiplier = 1.5f;
	private float _currentDamageMultiplier = 1f;
	public event IComboSeriesParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplySeriesDamage;
	public event Action<GameObject, Skill> OnSeriesDamaged;

	public float EnergyCostOnHit => EnergyPerAttack;

	public void OnSeriesHit(int hitCountInCurrentSeries, Character target)
	{
	}

	public void OnSeriesCompleted(Character target, int totalHits, float totalEnergySpent)
	{
		_currentDamageMultiplier = _bonusDamageMultiplier;
		PushBackEnemy(target);
	}

	public void OnSeriesBroken(Character target)
	{
		_currentDamageMultiplier = 1;
	}

	public void OnSeriesPotentialFinal(Skill skill, bool isPotentialFinal)
	{
	}

	private void PushBackEnemy(Character enemy)
	{
		if (enemy == null) return;

		Vector3 direction = (enemy.transform.position - Hero.transform.position).normalized;
		direction.y = 0;

		Vector3 pushPosition = enemy.transform.position + direction * KnockbackDistance;

		CmdKnockback(enemy.gameObject, pushPosition);
	}

	[Command]
	private void CmdKnockback(GameObject target, Vector3 pushPosition)
	{
		if (target == null) return;

		var moveComponent = target.GetComponent<MoveComponent>();
		if (moveComponent != null)
		{
			moveComponent.RpcDoPush(pushPosition, KnockbackDuration);
		}
		else
		{
			Debug.LogWarning($"[PhysicalAttack] MoveComponent not found on {target.name}");
		}
	}

	#endregion

    public bool IsStreamSkill { get; }
    public bool IsFrostEnergyApplied { get; }
}

