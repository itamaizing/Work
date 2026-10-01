using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class BlockOfIce : Skill, IEnergyDamagable, IComboSeriesParticipatingSkill
{
	[SerializeField] private BlockOfIceProjectile _iceArrow;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private float _runeCost = 1f;
	[SerializeField] private float _energyStep = 5f;
	[SerializeField] private float _damagePerStep = 3f;
	[SerializeField] private float _maxEnergySpend = 25f;

	private Vector3 _mousePos = Vector3.positiveInfinity;
	private Energy _energy;
	private RuneComponent _rune;

	private float _baseDamageMin = 20f;
	private float _baseDamageMax = 25f;

	private bool _isSeriesComplete;

	private const int ShardCount = 5;
	private const float ShardSpreadAngle = 120f;
	private const float ShardRange = 3f;
	private const float ShardDamagePercent = 0.3f;

	private float _currentEnergyCost;
	private float _bonusDamage;
	
	private float _delayStartTime;
	private float _currentDelayDuration;

	public bool WasInterruptedForShadow { get; private set; }

	public struct BlockOfIceState
	{
		public float BonusDamage;
		public Vector3 TargetPosition;
	}

	protected override bool IsCanCast => IsCanCastCheck();
	protected override int AnimTriggerCastDelay => 0;
	protected override int AnimTriggerCast => 0;

	private bool IsCanCastCheck()
	{
		return true;
	}

	public override void Init(SkillRenderer render, Character hero)
	{
		base.Init(render, hero);
		if (_energy == null) _energy = (Energy)hero.Resources[ResourceType.Energy];
		if (_rune == null) _rune = (RuneComponent)hero.Resources[ResourceType.Rune];
	}

	public bool TryGetState(out BlockOfIceState state)
	{
		if (IsCasting || WasInterruptedForShadow)
		{
			state = new BlockOfIceState
			{
				BonusDamage = CalculateBonusDamage(),
				TargetPosition = Targeting.GetTarget() != null
					? Targeting.GetTarget().Position
					: transform.position + transform.forward * 5f
			};
			WasInterruptedForShadow = true;
			return true;
		}

		state = default;
		return false;
	}
	
	private void OnEnable()
	{
		CastDeleyStarted += OnCastDelayStarted;
	}

	private void OnDisable()
	{
		CastDeleyStarted -= OnCastDelayStarted;
	}

	private void OnCastDelayStarted(float duration)
	{
		_delayStartTime = Time.time;
		_currentDelayDuration = duration;
	}
	
	public float RemainingCastDelay
	{
		get
		{
			if (_castDeleyCoroutine != null)
			{
				float elapsed = Time.time - _delayStartTime;
				return Mathf.Max(0f, _currentDelayDuration - elapsed);
			}

			return 0f;
		}
	}
	
	public void CastFromShadowWithDelay(Vector3 shadowPosition, Quaternion shadowRotation, float bonusDamage, Vector3 targetPos, float delay)
	{
		StartCoroutine(CastFromShadowRoutine(shadowPosition, shadowRotation, bonusDamage, targetPos, delay));
	}
	
	private IEnumerator CastFromShadowRoutine(Vector3 shadowPosition, Quaternion shadowRotation, float bonusDamage, Vector3 targetPos, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
    
        CastFromShadow(shadowPosition, shadowRotation, bonusDamage, targetPos);
    }

	private void Shoot(float bonusDamage)
	{
		_mousePos = Targeting.GetTarget().Position;
		Vector3 lookDir = _mousePos - _playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;

		CmdCreateProjecttile(angle, bonusDamage);
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float bonusDamage)
	{
		BlockOfIceProjectile projectile = Instantiate(_iceArrow, transform.position, Quaternion.Euler(0, -angle, 0));

		float baseDamage = UnityEngine.Random.Range(_baseDamageMin, _baseDamageMax);
		float finalDamage = baseDamage + bonusDamage;

		projectile.Init(_playerLinks, finalDamage, false, this);

		NetworkServer.Spawn(projectile.gameObject);
		RpcInit(projectile.gameObject, finalDamage);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<BlockOfIceProjectile>().Init(_playerLinks, manaValue, false, this);
	}

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		WasInterruptedForShadow = false;
		return base.PrepareJob(callbackDataSaved);
	}

	protected override IEnumerator CastJob()
	{
		if (WasInterruptedForShadow) yield break;

		if (Targeting.GetTarget() == null || !Cost.TryPaySingle(_runeCost, ResourceType.Rune, shouldModify: false))
		{
			TryCancel(true);
			yield break;
		}

		_bonusDamage = CalculateBonusDamage();

		Shoot(_bonusDamage);

		yield return null;
		_currentEnergyCost = 0;
	}

	public void CastFromShadow(Vector3 shadowPosition, Quaternion shadowRotation, float bonusDamage, Vector3 targetPos)
	{
		Vector3 lookDir = targetPos - shadowPosition;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;

		BlockOfIceProjectile projectile = Instantiate(_iceArrow, shadowPosition, Quaternion.Euler(0, -angle, 0));
		float baseDamage = UnityEngine.Random.Range(_baseDamageMin, _baseDamageMax);
		float finalDamage = baseDamage + bonusDamage;

		projectile.Init(_playerLinks, finalDamage, false, this);

		NetworkServer.Spawn(projectile.gameObject);
		RpcInit(projectile.gameObject, finalDamage);
	}

	public void ResetShadowState()
	{
		WasInterruptedForShadow = false;
	}

	public void RegisterSeriesHit(GameObject targetGo)
	{
		RpcRegisterSeriesHit(targetGo);
	}

	[ClientRpc]
	private void RpcRegisterSeriesHit(GameObject targetGo)
	{
		OnSeriesDamaged?.Invoke(null, this);

		if (_isSeriesComplete)
		{
			_isSeriesComplete = false;
			float totalDamage = UnityEngine.Random.Range(_baseDamageMin, _baseDamageMax) + _bonusDamage;
			CmdSpawnShards(targetGo, totalDamage * ShardDamagePercent);
		}
	}

	[Command]
	private void CmdSpawnShards(GameObject targetGo, float shardDamage)
	{
		if (targetGo == null) return;
		Character target = targetGo.GetComponent<Character>();
		if (target == null) return;

		Vector3 origin = target.transform.position;
		Vector3 backDir = (origin - Hero.transform.position).normalized;
		float baseAngle = Mathf.Atan2(backDir.z, backDir.x) * Mathf.Rad2Deg;

		float angleStep = ShardSpreadAngle / (ShardCount - 1);
		float startAngle = baseAngle - ShardSpreadAngle / 2f;
		float spawnOffset = 0.8f;

		for (int i = 0; i < ShardCount; i++)
		{
			float angle = startAngle + angleStep * i;
			float rad = angle * Mathf.Deg2Rad;
			Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

			Vector3 spawnPos = origin + dir * spawnOffset;

			BlockOfIceProjectile shard = Instantiate(
				_iceArrow,
				spawnPos,
				Quaternion.LookRotation(dir));

			shard.Init(_playerLinks, shardDamage, false, this);
			shard.SetMaxDistance(ShardRange);

			NetworkServer.Spawn(shard.gameObject);
			RpcInit(shard.gameObject, shardDamage);
		}
	}

	private float CalculateBonusDamage()
	{
		float totalBonusDamage = 0f;

		for (int i = 0; i < _maxEnergySpend / _energyStep; i++)
		{
			if (!Cost.TryPaySingle(_energyStep, ResourceType.Energy, shouldModify: false))
				break;

			_currentEnergyCost += _energyStep;
			totalBonusDamage += _damagePerStep;
		}

		return totalBonusDamage;
	}

	protected override void ClearData()
	{
		_mousePos = Vector3.positiveInfinity;
		WasInterruptedForShadow = false;
	}

	public bool IsStreamSkill => false;
	public bool IsFrostEnergyApplied => true;

	#region Series

	public bool IsTicking => false;
	public event IComboSeriesParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplySeriesDamage;
	public event Action<GameObject, Skill> OnSeriesDamaged;
	public float EnergyCostOnHit => _currentEnergyCost;
	public float RuneCostOnHit => _runeCost;

	public void OnSeriesHit(int hitCountInCurrentSeries, Character target)
	{
	}

	public void OnSeriesCompleted(Character target, int totalHits, float totalEnergySpent)
	{
		_isSeriesComplete = true;
	}

	public void OnSeriesBroken(Character target)
	{
		_isSeriesComplete = false;
	}

	public void OnSeriesPotentialFinal(Skill skill, bool isPotentialFinal)
	{
	}

	#endregion
}