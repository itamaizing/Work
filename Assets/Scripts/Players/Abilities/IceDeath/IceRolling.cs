using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.AI;
using System;

public class IceRolling : Skill
{
	[Header("Ability properties")]

	[SerializeField] private Character _playerLinks;
	[SerializeField] private PhysicalAttack _physicalAttack;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private float _jumprange = 2f;
	[SerializeField] private float _durationOfJumpPerCell = 0.3f;
	[SerializeField] private AudioClip _audioClip;
	[SerializeField] private LayerMask _groundLayerMask;

	private static readonly int iceRollingStart = Animator.StringToHash("IceRollingStart");
	private static readonly int iceRollingEnd = Animator.StringToHash("IceRollingEnd");

	private AudioSource _audioSource;
	private Vector3 _mousePos = Vector2.positiveInfinity;

	//private Vector3 _jumpPos;
	private Vector3 _lookDir;
	private Energy _energy;

	private bool _rollingPhysTalent = false;
	private bool _rollingWithEnemyTalent = false;

	private bool _isLastInSeries = false;
	private bool _isJump = false;
	private float _pendingFrozenDurationFromRoll;

	private float _durationOfJump;
	private float _jumpCount = 0;
	private bool _afterJump;
	private float _afterJumpDelay = 1;
	private Character _attachedTarget;
	private Animator _animator;

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
	public void TalentRollingPhys(bool value) => _rollingPhysTalent = value;
	public void RollingWithEnemyTalentActive(bool value) => _rollingWithEnemyTalent = value;

	#endregion

	#region Constants
	private const float BoxCastSize = 0.05f;
	private const float ObstaclePushBackMultiplier = 1.2f;
	private const float EnergyChunkValue = 5f;
	private const float KnockbackDistance = 2f;
	private const float KnockbackDuration = 0.5f;
	private const float AttachedFrostingDuration = 2f;
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

	private void Start()
	{
		_animator = GetComponent<Animator>();
		_audioSource = GetComponent<AudioSource>();
	}

    private void Update()
	{
		if (_afterJump)
		{
			TimerDelay();
		}
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

	private bool CheckObstacleBetween(Vector3 start, Vector3 end, out Vector3 stopPosition, out Character characterHit)
	{
		Vector3 direction = (end - start).normalized;
		float distance = Vector3.Distance(start, end);

		RaycastHit[] hits = Physics.BoxCastAll(start, new Vector3(BoxCastSize, BoxCastSize, BoxCastSize), direction, Quaternion.identity, distance, _obstacle);

		stopPosition = end;
		characterHit = null;

		foreach (RaycastHit hit in hits)
		{
			if (hit.collider.TryGetComponent(out Character character))
			{
				if (character != _playerLinks)
                {
					if (!_rollingWithEnemyTalent && character != Targeting.GetTarget()?.Character)
					{
						stopPosition = hit.point - direction;
						characterHit = character;
						return true;
					}
				}
			}
			if (hit.collider.transform.root != transform.root)
			{
				Debug.Log(hit.collider.gameObject.name + " Stop point " + stopPosition);
				stopPosition = hit.point - direction * ObstaclePushBackMultiplier;
				return true;
			}
		}

		return false;
	}

	private bool IsTargetInCloseProximity(Vector3 start, Vector3 direction, out Character characterHit)
	{
		Ray ray = new Ray(start, direction);
		RaycastHit[] hits = Physics.RaycastAll(ray, 1);

		characterHit = null;

		foreach (RaycastHit hit in hits)
		{
			if (hit.collider.TryGetComponent(out Character character) && character == characterHit && character != _playerLinks)
			{
				characterHit = character;
				return true;
			}
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
		_isJump = true;
		_lookDir = (_mousePos - _playerLinks.transform.position).normalized;
		Vector3 startPosition = _playerLinks.transform.position;
		Vector3 rawTargetPos = _mousePos;

		float distanceToClick = Vector3.Distance(startPosition, rawTargetPos);
		float finalRange;
		int extraCells = 0;
		if (distanceToClick <= 2f)
		{
			finalRange = 2f;
			extraCells = 0;
		}
		else if (distanceToClick < 4f)
		{
			finalRange = distanceToClick;
			extraCells = Mathf.CeilToInt(finalRange) - 2;
		}
		else
		{
			finalRange = 4f;
			extraCells = 2;
		}

		float additionalCost = extraCells * 5f;

		_energy.CmdUse(additionalCost);
		if (_isLastInSeries && Targeting.GetTarget()?.Character == null && _rollingWithEnemyTalent)
			finalRange *= 1.5f;

		Vector3 jumpPos = startPosition + _lookDir * finalRange;

		Vector3 stopPosition;
		Character characterHit;
		Character characterHitTarget;
		bool hit = CheckObstacleBetween(_playerLinks.transform.position, jumpPos, out stopPosition, out characterHit);
		bool hitTarget = IsTargetInCloseProximity(_playerLinks.transform.position, _lookDir, out characterHitTarget);

		Hero.Move.LookAtPosition(jumpPos);
		float actualDistance = Vector3.Distance(startPosition, stopPosition);
		if (_isDamageAddFrosting)
		{
			int rolledCells = Mathf.RoundToInt(finalRange);
			_frozenDuration = 0.7f * rolledCells;
		}

		CmdPush(stopPosition, actualDistance);
		if (_rollingWithEnemyTalent && Targeting.GetTarget()?.Character != null && hitTarget && characterHitTarget != null) CmdPushWithCharacter(stopPosition, characterHitTarget, actualDistance);

		if (_rollingPhysTalent)
		{
			_physicalAttack.TalentRollingPhys(_afterJump, finalRange);
			_afterJump = true;
		}
		if (!_hero.Abilities.SkillQueue.Skills.Contains(this))
		{
			Targeting.ClearTarget();
			_mousePos = Vector3.positiveInfinity;
			_lookDir = Vector3.zero;
		}
		else
		{
			Targeting.FindTempTarget();
			_mousePos = Targeting.GetTarget()?.Character != null ? Targeting.GetTarget().Character.transform.position : Targeting.GetMousePoint();
		}
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
					if (IsAllyTarget(damageable) || damageable as Character == Hero) Targeting.ClearTempTarget();
					else candidatePoint = Targeting.GetTempTarget().Targetable.Transform.position;
				}

				else candidatePoint = GetMousePoint(_groundLayerMask);

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
			_skillRender.SetSizeBox(1, GetJumpRange());
			yield return new WaitForSeconds(time);
		}
	}

	protected override IEnumerator CastJob()
	{
		if (!float.IsInfinity(_mousePos.x))
        {
	        _isLastInSeries = _seriesOfStrikes.MakeHit(Targeting.GetTarget()?.Character, Info.AbilityForm, 1, 0, 0);
			Jump2();
			yield return null;
		}
	}

	protected override void ClearData()
	{
		Targeting.ClearTarget();
		Targeting.ClearTempTarget();
		_hero.Move.StopLookAt();
		if (!_hero.Abilities.SkillQueue.Skills.Contains(this))
		{
			Targeting.ClearTarget();
			//_target = null;
			_mousePos = Vector3.positiveInfinity;
		}
		_isJump = false;
		Hero.Move.StopLookAt();
	}

	private void HandleJumpEnd()
	{
		if (_animator != null)
		{
			_animator.ResetTrigger(iceRollingStart);
			_animator.SetTrigger(iceRollingEnd);
		}
	}

	private void TimerDelay()
	{
		_afterJumpDelay -= Time.deltaTime;
		if (_afterJumpDelay < 0)
		{
			_afterJumpDelay = 1;
			_afterJump = false;
			_physicalAttack.TalentRollingPhys(_afterJump, 0);
		}
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
		if (_attachedTarget)
        {
			_attachedTarget.CharacterState.AddState(States.Frosting, AttachedFrostingDuration, 0f, _playerLinks.gameObject, Name);
			_attachedTarget.transform.SetParent(null);
			RpcReleaseTarget(_attachedTarget);
			_attachedTarget = null;
        }
	}

	private void OnCollisionEnter(Collision collision)
    {
		if (_rollingWithEnemyTalent && _isJump && collision.collider.TryGetComponent(out Character character) && character != _playerLinks && ((1 << character.gameObject.layer) & _targetsLayers) != 0)
		{
			Vector3 pushDir = (character.transform.position - _playerLinks.transform.position).normalized;
			pushDir.y = 0;
			Vector3 pushTarget = character.transform.position + pushDir * KnockbackDistance;
			CmdKnockback(character, pushTarget);
		}
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
		_playerLinks.Move.TargetRpcDoMove(force, _durationOfJump);
		StartCoroutine(WaitForJumpEnd());
	}

	[Command]
	private void CmdPushWithCharacter(Vector3 force, Character target, float finalRange)
	{
		RpcPlayShotSound();
		_durationOfJump = finalRange * _durationOfJumpPerCell;

		Vector3 startPosPlayer = _playerLinks.transform.position;
		Vector3 startPosTarget = target.transform.position;

		Vector3 direction = (force - startPosPlayer).normalized;
		float distanceBetween = Vector3.Distance(startPosPlayer, startPosTarget);
		Vector3 finalTargetPos = startPosTarget + direction * distanceBetween;

		if (target.TryGetComponent(out MoveComponent moveTarget))
			moveTarget.TargetRpcDoPush(finalTargetPos, _durationOfJump);

		if (_playerLinks.TryGetComponent(out MoveComponent movePlayer))
			movePlayer.TargetRpcDoPush(force, _durationOfJump);

		StartCoroutine(WaitForJumpEnd());
	}

	[Command]
	private void CmdKnockback(Character target, Vector3 force)
	{
		if (target.TryGetComponent(out MoveComponent move))
		{
			
			if (target.connectionToClient != null) move.TargetRpcDoMove(force, KnockbackDuration);
			else move.RpcDoMove(force, KnockbackDuration);
		}
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
		if (target.TryGetComponent(out MoveComponent move)) move.SetCanMove(false);
		if (target.TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;

		if (target.TryGetComponent(out Rigidbody rb))
		{
			rb.isKinematic = true;
			rb.linearVelocity = Vector3.zero;
		}
		target.transform.SetParent(_playerLinks.transform);
	}
}
