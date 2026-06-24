using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.AI;
using System;
using System.Linq;

public class IceRolling : Skill,IComboSeriesParticipatingSkill
{
	[Header("Ability properties")]
	[SerializeField] private float _jumprange = 2f;
	[SerializeField] private float _durationOfJumpPerCell = 0.3f;
	[SerializeField] private AudioClip _audioClip;
	[SerializeField] private LayerMask _groundLayerMask;

	private static readonly int iceRollingStart = Animator.StringToHash("IceRollingStart");
	private static readonly int iceRollingEnd = Animator.StringToHash("IceRollingEnd");

	private AudioSource _audioSource;
	private Vector3 _mousePos = Vector2.positiveInfinity;

	private Vector3 _lookDir;
	private Energy _energy;

	private float _additionalCost;
	private float _currentRollRange = 0;
	
	private const float AttachDistance = 1.2f;
	
	private float _durationOfJump;
	private Character _attachedTarget;
	private Animator _animator;
	
	private Coroutine _pullCoroutine;
	private Character _capturedTarget;

	#region Talent

	private bool _isDamageAddFrosting;
	private bool _isAttackWithFrosenAddEvade;

	public void AttackWithFrosenAddEvade(bool value) => _isAttackWithFrosenAddEvade = value;

	#region NinjaTalent6

	private float _frozenDuration;
	public void DamageAddFrosting(bool value)
	{
		if(value == _isDamageAddFrosting) return;
		_isDamageAddFrosting = value;
		_frozenDuration = 0;
		foreach (var ability in _hero.Abilities.Abilities)
		{
			if(ability == this) continue;
			if (ability.Info.AbilityForm is AbilityForm.Physical or AbilityForm.Both && _isDamageAddFrosting)
			{
				ability.OnDamageApplied += AddFrostingToPhysical;
			}
			else if(ability.Info.AbilityForm is AbilityForm.Physical or AbilityForm.Both && !_isDamageAddFrosting)
			{
				ability.OnDamageApplied -= AddFrostingToPhysical;
			}
		}
	}
	private void AddFrostingToPhysical(GameObject target, Skill skill)
	{
		if(target == null) return;
		CmdAddFrostingToPhysical(target,_frozenDuration);
		_frozenDuration = 0;
	}

	[Command]
	private void CmdAddFrostingToPhysical(GameObject target,float duration)
	{
		if(duration <= 0) return;
		if(target == null) return;
		var character = target.GetComponent<Character>();
		character.CharacterState.AddState(States.Frosting,duration,0,Schools.Water,_hero.gameObject,"IceRolling");
	}

	#endregion

	#endregion

	#region Constants
	private const float BoxCastSize = 0.05f;
	private const float ObstaclePushBackMultiplier = 1.2f;
	private const float EnergyChunkValue = 5f;
	private const float DynamicRendererJobTime = 0.2f;
	private const float TargetSearchRadius = 0.5f;
	private const float RayCastDistance = 1000f;
	#endregion

	protected override bool IsCanCast
	{
		get
		{
			if (Targeting.GetTarget()?.Character != null) return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;
			else return true;
		}
	}

	private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

	protected override int AnimTriggerCastDelay => 0;
	protected override int AnimTriggerCast => iceRollingStart;

	public void IceRollingCast() => AnimStartCastCoroutine();
	public void IceRollingEnd() => AnimCastEnded();

	public override void Init(SkillRenderer render, Character hero)
	{
		base.Init(render, hero);
		_animator = GetComponent<Animator>();
		_audioSource = GetComponent<AudioSource>();
	}

	private void OnEnable()
	{
		if (Hero != null && Hero.Health != null) Hero.Health.OnBeforeDamage += HandleFrozenEvade;
	}

	private void OnDisable()
	{
		if (Hero != null && Hero.Health != null) Hero.Health.OnBeforeDamage -= HandleFrozenEvade;
	}

	private float GetJumpRange()
	{
        if (_energy == null)
            _energy = (Energy)Hero.Resources[ResourceType.Energy];
        float range = _jumprange;
		float energyCost = 1;
		for (int i = 0; i < 2; i++)
		{
			if (_energy.CurrentValue >= energyCost)
			{
				range += 1;
				energyCost += EnergyChunkValue;
			}
		}

		return range;
	}
	
	private float GetFinalJumpRange()
	{
		float range = GetJumpRange();

		if (_isSeriesCompletedThisCast || _isSeriesPotentialFinal)
			range *= _seriesRangeMultiplier;

		return range;
	}

	private bool CheckObstacleBetween(Vector3 start, Vector3 end, out Vector3 stopPosition, out Character characterHit)
	{
		Vector3 direction = (end - start).normalized;
		float distance = Vector3.Distance(start, end);

		RaycastHit[] hits = Physics.BoxCastAll(start, new Vector3(BoxCastSize, BoxCastSize, BoxCastSize), direction, Quaternion.identity, distance, _obstacle);

		stopPosition = end;
		characterHit = null;

		if (hits.Length == 0)
			return false;

		var sortedHits = hits.OrderBy(h => h.distance).ToArray();

		foreach (var hit in sortedHits)
		{
			if (hit.collider.transform.root == transform.root)
				continue;

			if (hit.collider.TryGetComponent<Character>(out Character character) && character != _hero)
			{
				characterHit = character;
				
				if (_shouldCaptureTarget)
				{
					continue;
				}
				else
				{
					stopPosition = hit.point - direction * ObstaclePushBackMultiplier;
					return true;
				}
			}
			stopPosition = hit.point - direction * ObstaclePushBackMultiplier;
			return true;
		}

		return false;
	}


	private void HandleFrozenEvade(ref Damage damage, Skill skill)
	{
		if (!_isAttackWithFrosenAddEvade) return;
		if (skill == null || skill.Hero == null) return;

		Character attacker = skill.Hero;

		var frozen = attacker.CharacterState.GetState(States.Frozen) as FrozenState;
		if (frozen == null) return;

		float slowPercent = frozen.CurrentAttackSlowPercent;
		float evadeChance = slowPercent * 40f;

		if (UnityEngine.Random.Range(0f, 100f) <= evadeChance)
		{
			damage.Value = 0f;
			Hero.Health.InvokeEvade();
		}
	}

	private void Jump2()
	{
		Hero.Move.SetCanMove(false);

		_lookDir = (_mousePos - _hero.transform.position).normalized;
		Vector3 startPosition = _hero.transform.position;

		float finalRange = GetFinalJumpRange();

		float distanceToClick = Vector3.Distance(startPosition, _mousePos);
		int extraCells = 0;
		float maxRangeWithBonus =
			4f * (_isSeriesCompletedThisCast || _isSeriesPotentialFinal ? _seriesRangeMultiplier : 1f);

		if (distanceToClick <= 2f)
		{
			finalRange = 2f;
			extraCells = 0;
		}
		else if (distanceToClick < maxRangeWithBonus)
		{
			finalRange = distanceToClick;
			extraCells = Mathf.CeilToInt(finalRange) - 2;
		}
		else
		{
			finalRange = maxRangeWithBonus;
			extraCells = 2;
		}

		_currentRollRange = finalRange;
		_additionalCost = extraCells * 5f;

		OnSeriesDamaged?.Invoke(gameObject, this);
		_energy.CmdUse(_additionalCost);

		Vector3 intendedJumpPos = startPosition + _lookDir * _currentRollRange;

		Vector3 stopPosition;
		Character characterHit;
		bool hitObstacle = CheckObstacleBetween(startPosition, intendedJumpPos, out stopPosition, out characterHit);

		Hero.Move.LookAtPosition(stopPosition);
		float actualDistance = Vector3.Distance(startPosition, stopPosition);

		if (_isDamageAddFrosting)
		{
			int rolledCells = Mathf.RoundToInt(actualDistance);
			_frozenDuration = 0.7f * rolledCells;
		}

		_shouldCaptureTarget = _isSeriesCompletedThisCast;
		_targetToCapture = null;

		if (hitObstacle && !_shouldCaptureTarget)
		{
			CmdPush(stopPosition, actualDistance);
		}
		else if (_shouldCaptureTarget)
		{
			if (characterHit != null)
			{
				_targetToCapture = characterHit;
			}
			else
			{
				_targetToCapture = GetBestTargetForCapture(startPosition, intendedJumpPos);
			}

			if (_targetToCapture != null)
				CmdPushWithCharacter(stopPosition, _targetToCapture, actualDistance);
			else
				CmdPush(stopPosition, actualDistance);
		}
		else
		{
			CmdPush(stopPosition, actualDistance);
		}

		_isSeriesCompletedThisCast = false;
		_isSeriesPotentialFinal = false;
		_additionalCost = 0;
		_currentRollRange = 0;
	}

	public override void LoadTargetData(TargetInfo targetInfo)
	{
		if (targetInfo != null)
		{
			if(targetInfo.Points.Count > 0)
			{
				_mousePos = targetInfo.Points[0];
			}
		}
		
	}
	
	private Character GetBestTargetForCapture(Vector3 start, Vector3 end)
	{
		Vector3 direction = (end - start).normalized;
		float distance = Vector3.Distance(start, end);

		RaycastHit[] hits = Physics.SphereCastAll(start, 1.0f, direction, distance + 1f, _targetsLayers);

		Character bestTarget = null;
		float bestScore = float.MaxValue;

		foreach (var hit in hits)
		{
			if (!hit.collider.TryGetComponent<Character>(out Character ch)) continue;
			if (ch == Hero || IsAllyTarget(ch)) continue;

			float distToTarget = Vector3.Distance(start, hit.point);

			if (distToTarget < bestScore)
			{
				bestScore = distToTarget;
				bestTarget = ch;
			}
		}

		if (bestTarget == null)
		{
			Collider[] nearby = Physics.OverlapSphere(end, 1.8f, _targetsLayers);
			foreach (var col in nearby)
			{
				if (col.TryGetComponent<Character>(out Character ch) && 
				    ch != Hero && 
				    !IsAllyTarget(ch))
				{
					bestTarget = ch;
					break;
				}
			}
		}
		return bestTarget;
	}

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		Vector3 candidatePoint = Vector3.positiveInfinity;

		while (float.IsPositiveInfinity(candidatePoint.x))
		{
			if (GetMouseButton)
			{
				Targeting.FindTempTarget(Targeting.GetMousePoint(), TargetSearchRadius);

				if (Targeting.GetTempTarget()?.Targetable != null && Targeting.GetTempTarget()?.Targetable is IDamageable damageable)
				{
					if (IsAllyTarget(damageable) || damageable as Character == Hero)
						Targeting.ClearTempTarget();
					else
						candidatePoint = Targeting.GetTempTarget().Targetable.Transform.position;
				}
				else
					candidatePoint = GetMousePoint(_groundLayerMask);
			}
			yield return null;
		}

		TargetInfo targetInfo = new TargetInfo();
		targetInfo.Points.Add(candidatePoint);
		callbackDataSaved(targetInfo);
	}

	public override IEnumerator CustomDrawJob(float time = DynamicRendererJobTime)
	{
		while (IsPreparing)
		{
			float displayRange = GetFinalJumpRange();

			_skillRender.SetSizeBox(1, displayRange);

			yield return new WaitForSeconds(time);
		}
	}

	protected override IEnumerator CastJob()
	{
		Jump2();
		yield return null;
	}

	protected override void ClearData()
	{
		base.ClearData();

		Targeting.ClearTarget();
		Targeting.ClearTempTarget();
		_hero.Move.StopLookAt();
		_mousePos = Vector3.positiveInfinity;

		_currentRollRange = 0;
	}

	private void HandleJumpEnd()
	{
		if (_animator != null)
		{
			_animator.ResetTrigger(iceRollingStart);
			_animator.SetTrigger(iceRollingEnd);
		}

		_attachedTarget = null;
	}

	protected override bool CheckResourcesOnSkill()
	{
		bool result = base.CheckResourcesOnSkill(); 
		Cooldown.ForceEnd();
		return result;
	}

	private IEnumerator WaitForJumpEnd()
	{
		yield return new WaitForSeconds(_durationOfJump);

		RpcOnJumpEnd();

		if (_attachedTarget != null) 
		{
			_attachedTarget.transform.SetParent(null);
			RpcReleaseTarget(_attachedTarget);
			_attachedTarget = null;
		}

		_targetToCapture = null;
		_shouldCaptureTarget = false;
	}

	private Vector3 GetMousePoint(LayerMask mask)
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, RayCastDistance, mask)) return hit.point;

		return Vector3.positiveInfinity;
	}

	[Command]
	private void CmdPush(Vector3 force, float finalRange)
	{
		RpcPlayShotSound();
		_durationOfJump = finalRange * _durationOfJumpPerCell;
		force.y = 1;
		_hero.Move.TargetRpcDoMove(force, _durationOfJump);
		StartCoroutine(WaitForJumpEnd());
	}

	[Command]
	private void CmdPushWithCharacter(Vector3 force, Character target, float finalRange)
	{
		if (target == null || target == Hero)
		{
			CmdPush(force, finalRange);
			return;
		}

		RpcPlayShotSound();
		_durationOfJump = finalRange * _durationOfJumpPerCell;
		_attachedTarget = target;

		if (_hero.TryGetComponent(out MoveComponent movePlayer))
			movePlayer.TargetRpcDoPush(force, _durationOfJump);

		RpcAttachTarget(target);
		RpcPullTargetToPlayer(target.netId, force, _durationOfJump);

		StartCoroutine(WaitForJumpEnd());
	}

	[ClientRpc]
	private void RpcPullTargetToPlayer(uint targetNetId, Vector3 playerEndPos, float duration)
	{
		var targetObj = NetworkClient.spawned[targetNetId];
		if (targetObj == null) return;

		Character target = targetObj.GetComponent<Character>();
		if (target == null) return;

		if (_pullCoroutine != null) StopCoroutine(_pullCoroutine);
		_pullCoroutine = StartCoroutine(PullTargetCoroutine(target, playerEndPos, duration));
	}

	private IEnumerator PullTargetCoroutine(Character target, Vector3 playerEndPos, float duration)
	{
		if (target == null) yield break;

		Vector3 startTargetPos = target.transform.position;
		Vector3 startPlayerPos = _hero.transform.position;

		float timer = 0f;
		var targetMove = target.GetComponent<MoveComponent>();
		if (targetMove != null) targetMove.SetCanMove(false);

		const float FinalAttachDistance = 1.2f;

		while (timer < duration)
		{
			timer += Time.deltaTime;
			float t = timer / duration;

			Vector3 currentPlayerPos = Vector3.Lerp(startPlayerPos, playerEndPos, t);

			Vector3 dirToTarget = (startTargetPos - startPlayerPos).normalized;
			float currentDistance = Mathf.Lerp(Vector3.Distance(startTargetPos, startPlayerPos), FinalAttachDistance, t * 1.4f);

			Vector3 desiredTargetPos = currentPlayerPos + dirToTarget * currentDistance;

			target.transform.position = Vector3.Lerp(target.transform.position, desiredTargetPos, 12f * Time.deltaTime);

			yield return null;
		}

		target.transform.position = _hero.transform.position + _hero.transform.forward * FinalAttachDistance;

		if (targetMove != null) 
			targetMove.SetCanMove(true);

		_capturedTarget = null;
	}

	[ClientRpc]
	private void RpcReleaseTarget(Character target)
	{
		target.Move.SetCanMove(true);
		target.transform.SetParent(null);

		if (target.TryGetComponent(out NavMeshAgent agent))
		{
			agent.enabled = true;
			agent.Warp(Targeting.GetTarget().Character.transform.position);
		}

		if (target.TryGetComponent(out Rigidbody rigidbody)) rigidbody.isKinematic = false;
	}

	[ClientRpc] private void RpcPlayShotSound() => _audioSource?.PlayOneShot(_audioClip);
	[ClientRpc] private void RpcOnJumpEnd() => HandleJumpEnd();

	[ClientRpc]
	private void RpcAttachTarget(Character target)
	{
		if (target == null) return;

		if (target.TryGetComponent(out MoveComponent move)) move.SetCanMove(false);
		if (target.TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;
		if (target.TryGetComponent(out Rigidbody rb))
		{
			rb.isKinematic = true;
			rb.linearVelocity = Vector3.zero;
		}

		target.transform.SetParent(_hero.transform);
		target.transform.localPosition = new Vector3(0, 0.4f, 1.2f);
		target.transform.localRotation = Quaternion.identity;
	}

	#region Series

	private Character _targetToCapture;
	private bool _shouldCaptureTarget = false;
	
	private float _seriesRangeMultiplier = 1.5f;
	private float _jumpMultiplier = 1.5f;
	
	private bool _isSeriesPotentialFinal = false;
	private bool _isSeriesCompletedThisCast = false;
	public event IComboSeriesParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplySeriesDamage;
	public event Action<GameObject, Skill> OnSeriesDamaged;

	public float EnergyCostOnHit => _additionalCost + Cost.BaseCost;

	public void OnSeriesHit(int hitCountInCurrentSeries, Character target) { }

	public void OnSeriesCompleted(Character target, int totalHits, float totalEnergySpent)
	{
		_isSeriesCompletedThisCast = true;
	}

	public void OnSeriesBroken(Character target)
	{
		_isSeriesCompletedThisCast = false;
		_isSeriesPotentialFinal = false;
	}

	public void OnSeriesPotentialFinal(Skill skill, bool isPotentialFinal)
	{
		_isSeriesPotentialFinal = isPotentialFinal;
	}

	#endregion
	
}
